namespace kecas.application.fares.CreateFare;

using kecas.domain.network;

/// <summary>
/// POST /fares (FR-08): propose a fare for one route XOR one category-tagged
/// from/to zone pair. Starts in DRAFT; submit moves it to PENDING_APPROVAL (§5.2).
/// </summary>
public sealed record CreateFareCommand(
    Guid ActorId,
    Guid? RouteId,
    RouteCategory? Category,
    Guid? FromZoneId,
    Guid? ToZoneId,
    int AmountKobo,
    DateOnly EffectiveFrom) : ICommand<Guid>;
