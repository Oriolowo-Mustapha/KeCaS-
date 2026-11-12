namespace kecas.application.abstractions.messaging;

using MediatR;

/// <summary>A command that mutates state and returns only success/failure.</summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>A command that mutates state and returns a value on success.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
