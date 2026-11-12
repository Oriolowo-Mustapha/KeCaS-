namespace kecas.domain.fleet;

/// <summary>
/// A transport organisation that owns vehicles: university-run, a cooperative,
/// or an independent operator. Operators may only see and change records
/// belonging to their own organisation ("own org" scope, design doc §8.1).
/// </summary>
public sealed class Operator : Entity
{
    private Operator(Guid id, string name, OperatorKind kind, string? phone, bool isActive)
        : base(id)
    {
        Name = name;
        Kind = kind;
        Phone = phone;
        IsActive = isActive;
    }

    private Operator()
    {
        // EF Core
    }

    public string Name { get; private set; } = string.Empty;

    public OperatorKind Kind { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Operator> Create(string name, OperatorKind kind, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Operator>(OperatorErrors.NameRequired);
        }

        if (name.Trim().Length > 120)
        {
            return Result.Failure<Operator>(OperatorErrors.NameTooLong);
        }

        if (!Enum.IsDefined(kind))
        {
            return Result.Failure<Operator>(OperatorErrors.KindInvalid);
        }

        return new Operator(
            Guid.NewGuid(),
            name.Trim(),
            kind,
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            isActive: true);
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(OperatorErrors.AlreadyInactive);
        }

        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
        {
            return Result.Failure(OperatorErrors.AlreadyActive);
        }

        IsActive = true;
        return Result.Success();
    }

    public Result Update(string name, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(OperatorErrors.NameRequired);
        }

        if (name.Trim().Length > 120)
        {
            return Result.Failure(OperatorErrors.NameTooLong);
        }

        Name = name.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        return Result.Success();
    }
}
