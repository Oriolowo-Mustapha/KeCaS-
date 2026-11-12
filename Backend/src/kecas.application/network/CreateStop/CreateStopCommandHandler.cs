namespace kecas.application.network.CreateStop;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class CreateStopCommandHandler(
    IZoneRepository zoneRepository,
    IStopRepository stopRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateStopCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateStopCommand command, CancellationToken cancellationToken)
    {
        if (await zoneRepository.GetByIdAsync(command.ZoneId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(NetworkErrors.ZoneNotFound);
        }

        var stopResult = Stop.Create(command.Code, command.Name, command.ZoneId, command.IsRank);
        if (stopResult.IsFailure)
        {
            return Result.Failure<Guid>(stopResult.Error);
        }

        if (await stopRepository.CodeExistsAsync(stopResult.Value.Code, cancellationToken))
        {
            return Result.Failure<Guid>(NetworkErrors.StopCodeDuplicate);
        }

        stopRepository.Add(stopResult.Value);

        auditWriter.Append(
            command.ActorId,
            "stop.created",
            "stop",
            stopResult.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                code = stopResult.Value.Code,
                name = stopResult.Value.Name,
                zoneId = stopResult.Value.ZoneId,
                isRank = stopResult.Value.IsRank,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return stopResult.Value.Id;
    }
}
