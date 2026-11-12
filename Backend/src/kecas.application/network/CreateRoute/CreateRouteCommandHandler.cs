namespace kecas.application.network.CreateRoute;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class CreateRouteCommandHandler(
    IRouteRepository routeRepository,
    IStopRepository stopRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRouteCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRouteCommand command, CancellationToken cancellationToken)
    {
        var routeResult = Route.Create(command.Code, command.Name, command.Category, command.Description);
        if (routeResult.IsFailure)
        {
            return Result.Failure<Guid>(routeResult.Error);
        }

        if (await routeRepository.CodeExistsAsync(routeResult.Value.Code, cancellationToken))
        {
            return Result.Failure<Guid>(NetworkErrors.RouteCodeDuplicate);
        }

        var stopsResult = await ValidateAndSetStopsAsync(routeResult.Value, command.Stops, stopRepository, cancellationToken);
        if (stopsResult.IsFailure)
        {
            return Result.Failure<Guid>(stopsResult.Error);
        }

        routeRepository.Add(routeResult.Value);

        auditWriter.Append(
            command.ActorId,
            "route.created",
            "route",
            routeResult.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                code = routeResult.Value.Code,
                name = routeResult.Value.Name,
                category = routeResult.Value.Category.ToString().ToUpperInvariant(),
                stops = routeResult.Value.Stops.Select(s => new
                {
                    stopId = s.StopId,
                    seq = s.Seq,
                    minsFromStart = s.MinsFromStart,
                }),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return routeResult.Value.Id;
    }

    /// <summary>
    /// Shared guard for create and replace: every referenced stop must exist
    /// before the route's own sequence validation runs.
    /// </summary>
    internal static async Task<Result> ValidateAndSetStopsAsync(
        Route route,
        IReadOnlyList<RouteStopInput> stops,
        IStopRepository stopRepository,
        CancellationToken cancellationToken)
    {
        var existingIds = await stopRepository.ListByIdsAsync(
            stops.Select(s => s.StopId).ToList(), cancellationToken);
        var existingIdSet = existingIds.Select(s => s.Id).ToHashSet();

        if (stops.Any(s => !existingIdSet.Contains(s.StopId)))
        {
            return Result.Failure(NetworkErrors.StopNotFound);
        }

        return route.SetStops(stops.Select(s => (s.StopId, s.Seq, s.MinsFromStart)).ToList());
    }
}
