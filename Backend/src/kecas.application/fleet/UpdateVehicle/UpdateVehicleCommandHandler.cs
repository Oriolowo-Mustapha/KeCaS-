namespace kecas.application.fleet.UpdateVehicle;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class UpdateVehicleCommandHandler(
    IVehicleRepository vehicleRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateVehicleCommand>
{
    public async Task<Result> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure(VehicleErrors.NotFound);
        }

        var before = JsonSerializer.Serialize(new { capacity = vehicle.Capacity });

        if (command.Capacity.HasValue)
        {
            var result = vehicle.UpdateCapacity(command.Capacity.Value);
            if (result.IsFailure)
            {
                return result;
            }
        }

        auditWriter.Append(
            command.ActorId,
            "vehicle.updated",
            "vehicle",
            vehicle.Id.ToString(),
            before,
            JsonSerializer.Serialize(new { capacity = vehicle.Capacity }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
