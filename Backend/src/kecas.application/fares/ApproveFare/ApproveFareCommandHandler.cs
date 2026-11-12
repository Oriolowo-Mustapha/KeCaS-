namespace kecas.application.fares.ApproveFare;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.fares;

internal sealed class ApproveFareCommandHandler(
    IFareRepository fareRepository,
    IAuditWriter auditWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ApproveFareCommand>
{
    public async Task<Result> Handle(ApproveFareCommand command, CancellationToken cancellationToken)
    {
        var fare = await fareRepository.GetByIdAsync(command.FareId, cancellationToken);
        if (fare is null)
        {
            return Result.Failure(FareErrors.NotFound);
        }

        var supersededFare = await fareRepository.FindPublishedForScopeAsync(
            fare.RouteId, fare.FromZoneId, fare.ToZoneId, cancellationToken);

        // §5.3: supersede + publish in ONE transaction. The published-fare
        // unique index is checked per statement, so the supersede UPDATE must
        // be flushed before the new fare becomes PUBLISHED — the domain
        // Approve call happens AFTER the first save, not before it.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (supersededFare is not null && supersededFare.Id != fare.Id)
            {
                // The old fare ends on the day the new fare becomes effective.
                var supersedeResult = supersededFare.Supersede(fare.EffectiveFrom);
                if (supersedeResult.IsFailure)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return supersedeResult;
                }

                auditWriter.Append(
                    command.ActorId,
                    "fare.superseded",
                    "fare",
                    supersededFare.Id.ToString(),
                    before: JsonSerializer.Serialize(new { status = "PUBLISHED" }),
                    after: JsonSerializer.Serialize(new
                    {
                        status = "SUPERSEDED",
                        supersededBy = fare.Id,
                        effectiveTo = supersededFare.EffectiveTo,
                    }));

                // Flush the supersede UPDATE first: only the old fare is dirty here.
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // §5.3: the approver may not be the proposer; the domain guard
            // returns FareErrors.SelfApproval which maps to 403.
            var approveResult = fare.Approve(command.ActorId, dateTimeProvider.UtcNow);
            if (approveResult.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return approveResult;
            }

            if (supersededFare is not null)
            {
                fare.MarkSupersedes(supersededFare.Id);
            }

            auditWriter.Append(
                command.ActorId,
                "fare.approved",
                "fare",
                fare.Id.ToString(),
                before: JsonSerializer.Serialize(new { status = "PENDING_APPROVAL" }),
                after: JsonSerializer.Serialize(new
                {
                    status = fare.Status.ToStorageValue(),
                    approvedBy = fare.ApprovedBy,
                    amountKobo = fare.Amount.AmountInKobo,
                }));

            // Now the new fare becomes PUBLISHED — no conflict remains.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return Result.Success();
    }
}
