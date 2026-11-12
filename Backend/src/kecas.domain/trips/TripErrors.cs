namespace kecas.domain.trips;

public static class TripErrors
{
    public static readonly Error NotFound = new(
        "Trip.NotFound",
        "The specified trip was not found.");

    public static readonly Error CodeDuplicate = new(
        "Trip.CodeDuplicate",
        "A trip with this code already exists.");

    public static readonly Error EndBeforeStart = new(
        "Trip.EndBeforeStart",
        "A trip cannot end before it starts.");

    public static readonly Error RouteInactive = new(
        "Trip.RouteInactive",
        "The route is not active; trips cannot be scheduled on it.");

    public static readonly Error VehicleNotAssignable = new(
        "Trip.VehicleNotAssignable",
        "The vehicle must be ACTIVE and match the route's category.");

    public static readonly Error DriverNotAssignable = new(
        "Trip.DriverNotAssignable",
        "The driver must be ACTIVE with an unexpired licence.");

    public static readonly Error TripOverlap = new(
        "Trip.TripOverlap",
        "The driver or vehicle already has a live trip overlapping these times.");

    public static readonly Error NotStartable = new(
        "Trip.NotStartable",
        "Only scheduled trips can be started.");

    public static readonly Error NotCompletable = new(
        "Trip.NotCompletable",
        "Only trips in progress can be completed.");

    public static readonly Error NotCancellable = new(
        "Trip.NotCancellable",
        "Only scheduled or in-progress trips can be cancelled.");

    public static readonly Error ReasonRequired = new(
        "Trip.ReasonRequired",
        "A reason is required to cancel a trip.");

    public static readonly Error ShiftRequired = new(
        "Trip.ShiftRequired",
        "The vehicle must be ACTIVE and on an open shift with the same driver to start a trip.");

    public static readonly Error RowVersionConflict = new(
        "Trip.RowVersionConflict",
        "The trip was changed by someone else; reload and retry.");

    public static readonly Error NotEditable = new(
        "Trip.NotEditable",
        "Only scheduled trips can be edited.");
}
