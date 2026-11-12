namespace kecas.application.abstractions.messaging;

using MediatR;

/// <summary>A query that reads data and never mutates state.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
