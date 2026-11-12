namespace kecas.application.network.CreateZone;

/// <summary>POST /zones (FR-07): create a fare area, e.g. Z1 Campus.</summary>
public sealed record CreateZoneCommand(
    Guid ActorId,
    string Code,
    string Name) : ICommand<Guid>;
