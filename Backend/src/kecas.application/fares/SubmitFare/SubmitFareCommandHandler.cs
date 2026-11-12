namespace kecas.application.fares.SubmitFare;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class SubmitFareCommandHandler(
    IFareRepository fareRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitFareCommand>
{
    public async Task<Result> Handle(SubmitFareCommand command, CancellationToken cancellationToken)
    {
        var fare = await fareRepository.GetByIdAsync(command.FareId, cancellationToken);
        if (fare is null)
        {
            return Result.Failure(FareErrors.NotFound);
        }

        var result = fare.SubmitForApproval();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "fare.submitted",
            "fare",
            fare.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "DRAFT" }),
            after: JsonSerializer.Serialize(new { status = fare.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
