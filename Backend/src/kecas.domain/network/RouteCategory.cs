namespace kecas.domain.network;

/// <summary>
/// How a route is served. Route categories match the vehicle categories
/// (design doc §4.2): a keke route is served by kekes, etc.
/// </summary>
public enum RouteCategory
{
    Keke = 1,
    Cab = 2,
    Shuttle = 3,
}
