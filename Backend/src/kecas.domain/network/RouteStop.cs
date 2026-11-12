namespace kecas.domain.network;

using kecas.domain.abstractions;

/// <summary>
/// Join entity: a stop's position on a route. Composite key
/// (route_id, stop_id); (route_id, seq) is unique (design doc §4.2).
/// MinsFromStart is the scheduled offset from the first stop.
/// </summary>
public sealed class RouteStop : Entity
{
    private RouteStop(Guid id, Guid routeId, Guid stopId, int seq, int minsFromStart)
        : base(id)
    {
        RouteId = routeId;
        StopId = stopId;
        Seq = seq;
        MinsFromStart = minsFromStart;
    }

    private RouteStop()
    {
        // EF Core
    }

    public Guid RouteId { get; private set; }

    public Guid StopId { get; private set; }

    /// <summary>1-based order along the route.</summary>
    public int Seq { get; private set; }

    /// <summary>Minutes after the route's first stop.</summary>
    public int MinsFromStart { get; private set; }

    internal static RouteStop FromSequence(Guid routeId, Guid stopId, int seq, int minsFromStart) =>
        new(Guid.NewGuid(), routeId, stopId, seq, minsFromStart);
}
