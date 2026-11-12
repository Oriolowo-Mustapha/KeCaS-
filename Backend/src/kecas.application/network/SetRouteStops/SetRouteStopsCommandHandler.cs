namespace kecas.application.network.SetRouteStops;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.application.network.CreateRoute;

internal sealed class SetRouteStopsCommandHandler(
    IRouteRepository routeRepository,
    IStopRepository stopRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetRouteStopsCommand>
{
    public async Task<Result> Handle(SetRouteStopsCommand command, CancellationToken cancellationToken)
    {
        var route = await routeRepository.GetByIdAsync(command.RouteId, cancellationToken);
        if (route is null)
        {
            return Result.Failure(NetworkErrors.RouteNotFound);
        }

        var before = JsonSerializer.Serialize(new
        {
            stops = route.Stops.OrderBy(s => s.Seq).Select(s => new
            {
                stopId = s.StopId,
                seq = s.Seq,
                minsFromStart = s.MinsFromStart,
            }),
        });

        var result = await CreateRouteCommandHandler.ValidateAndSetStopsAsync(
            route, command.Stops, stopRepository, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "route.stops_set",
            "route",
            route.Id.ToString(),
            before,
            JsonSerializer.Serialize(new
            {
                stops = route.Stops.OrderBy(s => s.Seq).Select(s => new
                {
                    stopId = s.StopId,
                    seq = s.Seq,
                    minsFromStart = s.MinsFromStart,
                }),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
