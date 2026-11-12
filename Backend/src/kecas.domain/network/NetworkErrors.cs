namespace kecas.domain.network;

public static class NetworkErrors
{
    public static readonly Error ZoneCodeRequired = new(
        "Zone.CodeRequired",
        "A zone code is required.");

    public static readonly Error ZoneCodeTooLong = new(
        "Zone.CodeTooLong",
        "The zone code may not exceed 10 characters.");

    public static readonly Error ZoneCodeDuplicate = new(
        "Zone.CodeDuplicate",
        "A zone with this code already exists.");

    public static readonly Error ZoneNameRequired = new(
        "Zone.NameRequired",
        "A zone name is required.");

    public static readonly Error ZoneNameTooLong = new(
        "Zone.NameTooLong",
        "The zone name may not exceed 120 characters.");

    public static readonly Error ZoneRequired = new(
        "Zone.Required",
        "A stop must belong to a zone.");

    public static readonly Error ZoneNotFound = new(
        "Zone.NotFound",
        "The specified zone was not found.");

    public static readonly Error StopCodeRequired = new(
        "Stop.CodeRequired",
        "A stop code is required.");

    public static readonly Error StopCodeTooLong = new(
        "Stop.CodeTooLong",
        "The stop code may not exceed 10 characters.");

    public static readonly Error StopCodeDuplicate = new(
        "Stop.CodeDuplicate",
        "A stop with this code already exists.");

    public static readonly Error StopNameRequired = new(
        "Stop.NameRequired",
        "A stop name is required.");

    public static readonly Error StopNameTooLong = new(
        "Stop.NameTooLong",
        "The stop name may not exceed 120 characters.");

    public static readonly Error StopNotFound = new(
        "Stop.NotFound",
        "The specified stop was not found.");

    public static readonly Error RouteCodeRequired = new(
        "Route.CodeRequired",
        "A route code is required.");

    public static readonly Error RouteCodeTooLong = new(
        "Route.CodeTooLong",
        "The route code may not exceed 16 characters.");

    public static readonly Error RouteCodeDuplicate = new(
        "Route.CodeDuplicate",
        "A route with this code already exists.");

    public static readonly Error RouteNameRequired = new(
        "Route.NameRequired",
        "A route name is required.");

    public static readonly Error RouteNameTooLong = new(
        "Route.NameTooLong",
        "The route name may not exceed 120 characters.");

    public static readonly Error RouteNotFound = new(
        "Route.NotFound",
        "The specified route was not found.");

    public static readonly Error RouteStopsRequired = new(
        "Route.StopsRequired",
        "A route needs at least one stop.");

    public static readonly Error RouteStopSequenceInvalid = new(
        "Route.StopSequenceInvalid",
        "Stop sequence numbers must be unique and ascending starting at 1.");

    public static readonly Error RouteStopMinutesInvalid = new(
        "Route.StopMinutesInvalid",
        "Minutes from the start stop cannot be negative.");
}
