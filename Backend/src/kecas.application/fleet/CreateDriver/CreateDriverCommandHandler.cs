namespace kecas.application.fleet.CreateDriver;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class CreateDriverCommandHandler(
    IDriverRepository driverRepository,
    IOperatorRepository operatorRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateDriverCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDriverCommand command, CancellationToken cancellationToken)
    {
        var operatorExists = await operatorRepository.ExistsAsync(command.OperatorId, cancellationToken);
        if (!operatorExists)
        {
            return Result.Failure<Guid>(OperatorErrors.NotFound);
        }

        var driverResult = Driver.Create(
            command.UserId,
            command.OperatorId,
            command.LicenceNumber,
            command.LicenceExpiry);
        if (driverResult.IsFailure)
        {
            return Result.Failure<Guid>(driverResult.Error);
        }

        var driver = driverResult.Value;
        driverRepository.Add(driver);

        auditWriter.Append(
            command.ActorId,
            "driver.created",
            "driver",
            driver.Id.ToString(),
            before: null,
            // FR-05/NFR-05: the licence number never appears in audit output.
            after: JsonSerializer.Serialize(new
            {
                operatorId = driver.OperatorId,
                licenceMasked = LicenceMasker.Mask(driver.LicenceNumber),
                licenceExpiry = driver.LicenceExpiry.ToString("yyyy-MM-dd"),
                status = driver.Status.ToString(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return driver.Id;
    }
}
