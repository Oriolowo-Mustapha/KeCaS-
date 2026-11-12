namespace kecas.application.fleet.UpdateDriver;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class UpdateDriverCommandHandler(
    IDriverRepository driverRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateDriverCommand>
{
    public async Task<Result> Handle(UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        var driver = await driverRepository.GetByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return Result.Failure(DriverErrors.NotFound);
        }

        var before = JsonSerializer.Serialize(new
        {
            licenceMasked = LicenceMasker.Mask(driver.LicenceNumber),
            licenceExpiry = driver.LicenceExpiry.ToString("yyyy-MM-dd"),
            status = driver.Status.ToString(),
        });

        if (command.LicenceNumber is not null || command.LicenceExpiry.HasValue)
        {
            var result = driver.UpdateLicence(
                command.LicenceNumber ?? driver.LicenceNumber,
                command.LicenceExpiry ?? driver.LicenceExpiry);
            if (result.IsFailure)
            {
                return result;
            }
        }

        if (command.Status.HasValue && command.Status.Value != driver.Status)
        {
            var result = command.Status.Value switch
            {
                DriverStatus.Active => driver.Reinstate(),
                DriverStatus.Suspended => driver.Suspend(),
                DriverStatus.Retired => driver.Retire(),
                _ => Result.Failure(DriverErrors.NotFound),
            };

            if (result.IsFailure)
            {
                return result;
            }
        }

        auditWriter.Append(
            command.ActorId,
            "driver.updated",
            "driver",
            driver.Id.ToString(),
            before,
            JsonSerializer.Serialize(new
            {
                licenceMasked = LicenceMasker.Mask(driver.LicenceNumber),
                licenceExpiry = driver.LicenceExpiry.ToString("yyyy-MM-dd"),
                status = driver.Status.ToString(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
