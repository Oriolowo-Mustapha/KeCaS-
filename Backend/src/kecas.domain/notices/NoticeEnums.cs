namespace kecas.domain.notices;

public enum NoticeAudience
{
    Everyone = 1,
    Passengers = 2,
    Drivers = 3,
    Staff = 4,
}

public enum NoticeCategory
{
    General = 1,
    Service = 2,
    Schedule = 3,
    Route = 4,
}

public static class NoticeEnums
{
    public static string ToStorageValue(this NoticeAudience audience) => audience switch
    {
        NoticeAudience.Everyone => "EVERYONE",
        NoticeAudience.Passengers => "PASSENGERS",
        NoticeAudience.Drivers => "DRIVERS",
        NoticeAudience.Staff => "STAFF",
        _ => audience.ToString().ToUpperInvariant(),
    };

    public static NoticeAudience NoticeAudienceFrom(string value) => value switch
    {
        "EVERYONE" => NoticeAudience.Everyone,
        "PASSENGERS" => NoticeAudience.Passengers,
        "DRIVERS" => NoticeAudience.Drivers,
        "STAFF" => NoticeAudience.Staff,
        _ => Enum.Parse<NoticeAudience>(value, ignoreCase: true),
    };

    public static string ToStorageValue(this NoticeCategory category) => category switch
    {
        NoticeCategory.General => "GENERAL",
        NoticeCategory.Service => "SERVICE",
        NoticeCategory.Schedule => "SCHEDULE",
        NoticeCategory.Route => "ROUTE",
        _ => category.ToString().ToUpperInvariant(),
    };

    public static NoticeCategory NoticeCategoryFrom(string value) => value switch
    {
        "GENERAL" => NoticeCategory.General,
        "SERVICE" => NoticeCategory.Service,
        "SCHEDULE" => NoticeCategory.Schedule,
        "ROUTE" => NoticeCategory.Route,
        _ => Enum.Parse<NoticeCategory>(value, ignoreCase: true),
    };
}
