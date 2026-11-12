namespace kecas.application.fares.RejectFare;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class RejectFareCommandHandler(
    IFareRepository fareRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RejectFareCommand>
{
    public async Task<Result> Handle(RejectFareCommand command, CancellationToken cancellationToken)
    {
        var fare = await fareRepository.GetByIdAsync(command.FareId, cancellationToken);
        if (fare is null)
        {
            return Result.Failure(FareErrors.NotFound);
        }

        var result = fare.Reject();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "fare.rejected",
            "fare",
            fare.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "PENDING_APPROVAL" }),
            after: JsonSerializer.Serialize(new { status = "REJECTED" }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
