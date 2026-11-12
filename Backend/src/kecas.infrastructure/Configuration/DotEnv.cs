namespace kecas.infrastructure.Configuration;

/// <summary>
/// Loads a .env file into process environment variables so that every
/// entry point (API, `dotnet ef`, tests) gets its configuration from one
/// place, as the team decided.
///
/// Rules:
/// - The file is searched from the current directory upwards (finds the
///   repo root's .env no matter which project is running).
/// - Lines are KEY=VALUE; '#' comments and blank lines are ignored;
///   surrounding single or double quotes are stripped.
/// - Variables already set in the real environment are NOT overridden,
///   so staging/production can still override local values.
///
/// .NET configuration uses double-underscore nesting, e.g.
///   ConnectionStrings__Database=...  ->  ConnectionStrings:Database
/// (On Linux the key case must match exactly.)
/// </summary>
public static class DotEnv
{
    public static void Load(string? startDirectory = null)
    {
        var path = FindEnvFile(startDirectory ?? Directory.GetCurrentDirectory());

        if (path is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindEnvFile(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent!;
        }

        return null;
    }
}
