namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.auth.CurrentUser;
using kecas.application.users;
using kecas.application.users.CreateUser;
using kecas.application.users.DeactivateUser;
using kecas.application.users.ListUsers;
using kecas.application.users.UpdateUser;
using MediatR;

/// <summary>
/// FR-03 / UC-08: account administration. Every route requires the
/// users:manage permission (§8.1: admin only).
/// </summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users");

        group.MapGet("", async (
            ISender sender,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListUsersQuery(page ?? 1, pageSize ?? 20), cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("users:manage")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("", async (
            CreateUserRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var command = new CreateUserCommand(
                actor.Id,
                request.FullName,
                request.Email,
                request.Phone,
                request.Password,
                request.Roles ?? []);

            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value}", new { id = result.Value })
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("users:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var command = new UpdateUserCommand(
                actor.Id,
                id,
                request.FullName,
                request.Phone,
                request.Roles);

            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.NoContent()
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("users:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(new DeactivateUserCommand(actor.Id, id), cancellationToken);
            return result.IsSuccess
                ? Results.NoContent()
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("users:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string? Phone,
    string Password,
    string[]? Roles);

public sealed record UpdateUserRequest(string? FullName, string? Phone, string[]? Roles);
