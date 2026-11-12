namespace kecas.application.fleet.CreateVehicle;

using kecas.application.abstractions.data;
using kecas.domain.fleet;

internal sealed class CreateVehicleCommandHandler(
    IVehicleRepository vehicleRepository,
    IOperatorRepository operatorRepository,
    IExistenceChecker existenceChecker,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateVehicleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var codeResult = VehicleCode.Create(command.Code, command.Category);
        if (codeResult.IsFailure)
        {
            return Result.Failure<Guid>(codeResult.Error);
        }

        var plateResult = PlateNumber.Create(command.PlateNumber);
        if (plateResult.IsFailure)
        {
            return Result.Failure<Guid>(plateResult.Error);
        }

        // NFR-07: vehicle identifiers must not be duplicated.
        if (await existenceChecker.VehiclePlateNumberExistsAsync(plateResult.Value.Value, cancellationToken))
        {
            return Result.Failure<Guid>(VehicleErrors.PlateDuplicate);
        }

        if (await existenceChecker.VehicleCodeExistsAsync(codeResult.Value.Value, cancellationToken))
        {
            return Result.Failure<Guid>(VehicleErrors.CodeDuplicate);
        }

        var operatorExists = await operatorRepository.ExistsAsync(command.OperatorId, cancellationToken);
        if (!operatorExists)
        {
            return Result.Failure<Guid>(OperatorErrors.NotFound);
        }

        var vehicleResult = Vehicle.Create(
            command.Code,
            command.PlateNumber,
            command.Category,
            command.Capacity,
            command.OperatorId);

        if (vehicleResult.IsFailure)
        {
            return Result.Failure<Guid>(vehicleResult.Error);
        }

        vehicleRepository.Add(vehicleResult.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return vehicleResult.Value.Id;
    }
}
