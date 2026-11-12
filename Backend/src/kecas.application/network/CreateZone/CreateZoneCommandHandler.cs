namespace kecas.application.network.CreateZone;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class CreateZoneCommandHandler(
    IZoneRepository zoneRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateZoneCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateZoneCommand command, CancellationToken cancellationToken)
    {
        var zoneResult = Zone.Create(command.Code, command.Name);
        if (zoneResult.IsFailure)
        {
            return Result.Failure<Guid>(zoneResult.Error);
        }

        if (await zoneRepository.CodeExistsAsync(zoneResult.Value.Code, cancellationToken))
        {
            return Result.Failure<Guid>(NetworkErrors.ZoneCodeDuplicate);
        }

        zoneRepository.Add(zoneResult.Value);

        auditWriter.Append(
            command.ActorId,
            "zone.created",
            "zone",
            zoneResult.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new { code = zoneResult.Value.Code, name = zoneResult.Value.Name }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return zoneResult.Value.Id;
    }
}
