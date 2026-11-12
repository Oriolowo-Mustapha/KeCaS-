namespace kecas.application.network.ListStops;

public sealed record ListStopsQuery(Guid? ZoneId) : IQuery<ListStopsResponse>;
