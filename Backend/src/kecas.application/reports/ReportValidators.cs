namespace kecas.application.reports;

using FluentValidation;

/// <summary>Report periods must go forward in time (only checked when both
/// bounds are given; a single bound is always valid).</summary>
internal sealed class TripsReportQueryValidator : AbstractValidator<TripsReportQuery>
{
    public TripsReportQueryValidator()
    {
        RuleFor(q => q.To).Must((q, to) => to is null || q.From is null || to > q.From);
    }
}

internal sealed class DriversReportQueryValidator : AbstractValidator<DriversReportQuery>
{
    public DriversReportQueryValidator()
    {
        RuleFor(q => q.To).Must((q, to) => to is null || q.From is null || to > q.From);
    }
}

internal sealed class AvailabilityHistoryReportQueryValidator : AbstractValidator<AvailabilityHistoryReportQuery>
{
    public AvailabilityHistoryReportQueryValidator()
    {
        RuleFor(q => q.To).Must((q, to) => to is null || q.From is null || to > q.From);
    }
}
