namespace kecas.application.network.UpdateRoute;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class UpdateRouteCommandHandler(
    IRouteRepository routeRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateRouteCommand>
{
    public async Task<Result> Handle(UpdateRouteCommand command, CancellationToken cancellationToken)
    {
        var route = await routeRepository.GetByIdAsync(command.RouteId, cancellationToken);
        if (route is null)
        {
            return Result.Failure(NetworkErrors.RouteNotFound);
        }

        var before = JsonSerializer.Serialize(new
        {
            name = route.Name,
            description = route.Description,
            isActive = route.IsActive,
        });

        if (command.Name is { } name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure(NetworkErrors.RouteNameRequired);
            }

            if (name.Trim().Length > 120)
            {
                return Result.Failure(NetworkErrors.RouteNameTooLong);
            }

            route.Rename(name);
        }

        if (command.Description is { } description)
        {
            route.SetDescription(string.IsNullOrWhiteSpace(description) ? null : description.Trim());
        }

        if (command.IsActive is { } isActive)
        {
            if (isActive)
            {
                route.Activate();
            }
            else
            {
                route.Deactivate();
            }
        }

        auditWriter.Append(
            command.ActorId,
            "route.updated",
            "route",
            route.Id.ToString(),
            before,
            JsonSerializer.Serialize(new
            {
                name = route.Name,
                description = route.Description,
                isActive = route.IsActive,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
