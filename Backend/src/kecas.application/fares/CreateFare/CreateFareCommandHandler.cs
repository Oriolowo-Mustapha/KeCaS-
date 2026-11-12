namespace kecas.application.fares.CreateFare;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class CreateFareCommandHandler(
    IFareRepository fareRepository,
    IRouteRepository routeRepository,
    IZoneRepository zoneRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateFareCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFareCommand command, CancellationToken cancellationToken)
    {
        if (command.RouteId is { } routeId && routeId != Guid.Empty)
        {
            if (await routeRepository.GetByIdAsync(routeId, cancellationToken) is null)
            {
                return Result.Failure<Guid>(NetworkErrors.RouteNotFound);
            }
        }

        if (command.FromZoneId is { } fromZoneId && fromZoneId != Guid.Empty
            && await zoneRepository.GetByIdAsync(fromZoneId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(NetworkErrors.ZoneNotFound);
        }

        if (command.ToZoneId is { } toZoneId && toZoneId != Guid.Empty
            && await zoneRepository.GetByIdAsync(toZoneId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(NetworkErrors.ZoneNotFound);
        }

        var fareResult = Fare.Create(
            command.RouteId,
            command.Category,
            command.FromZoneId,
            command.ToZoneId,
            new Money(command.AmountKobo),
            command.EffectiveFrom,
            command.ActorId);
        if (fareResult.IsFailure)
        {
            return Result.Failure<Guid>(fareResult.Error);
        }

        fareRepository.Add(fareResult.Value);

        auditWriter.Append(
            command.ActorId,
            "fare.created",
            "fare",
            fareResult.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                routeId = fareResult.Value.RouteId,
                category = fareResult.Value.Category is { } category
                    ? category.ToString().ToUpperInvariant()
                    : null,
                fromZoneId = fareResult.Value.FromZoneId,
                toZoneId = fareResult.Value.ToZoneId,
                amountKobo = fareResult.Value.Amount.AmountInKobo,
                effectiveFrom = fareResult.Value.EffectiveFrom,
                status = fareResult.Value.Status.ToStorageValue(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return fareResult.Value.Id;
    }
}
