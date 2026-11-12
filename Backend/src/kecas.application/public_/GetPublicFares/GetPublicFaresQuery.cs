namespace kecas.application.public_.GetPublicFares;

/// <summary>
/// GET /public/fares?category=&amp;from=&amp;to= (FR-09, DD-10): published fares
/// only, filterable by vehicle category and/or from/to zone codes.
/// </summary>
public sealed record GetPublicFaresQuery(string? Category, string? FromZoneCode, string? ToZoneCode)
    : IQuery<PublicFaresResponse>;
