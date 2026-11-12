namespace kecas.application.network.CreateStop;

/// <summary>POST /stops (FR-07): create a stop inside a zone; ranks mark where vehicles wait.</summary>
public sealed record CreateStopCommand(
    Guid ActorId,
    string Code,
    string Name,
    Guid ZoneId,
    bool IsRank) : ICommand<Guid>;
