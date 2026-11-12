namespace kecas.domain.payments;

/// <summary>Payment statuses per BR-08 (no gateway).</summary>
public enum PaymentStatus
{
    Pending = 1,
    Successful = 2,
    Failed = 3,
    Refunded = 4,
}

public enum PaymentMethod
{
    Cash = 1,
    Transfer = 2,
    Pos = 3,
    Other = 4,
}

public static class PaymentEnums
{
    public static string ToStorageValue(this PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "PENDING",
        PaymentStatus.Successful => "SUCCESSFUL",
        PaymentStatus.Failed => "FAILED",
        PaymentStatus.Refunded => "REFUNDED",
        _ => status.ToString().ToUpperInvariant(),
    };

    public static PaymentStatus PaymentStatusFrom(string value) => value switch
    {
        "PENDING" => PaymentStatus.Pending,
        "SUCCESSFUL" => PaymentStatus.Successful,
        "FAILED" => PaymentStatus.Failed,
        "REFUNDED" => PaymentStatus.Refunded,
        _ => Enum.Parse<PaymentStatus>(value, ignoreCase: true),
    };

    public static string ToStorageValue(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "CASH",
        PaymentMethod.Transfer => "TRANSFER",
        PaymentMethod.Pos => "POS",
        PaymentMethod.Other => "OTHER",
        _ => method.ToString().ToUpperInvariant(),
    };

    public static PaymentMethod PaymentMethodFrom(string value) => value switch
    {
        "CASH" => PaymentMethod.Cash,
        "TRANSFER" => PaymentMethod.Transfer,
        "POS" => PaymentMethod.Pos,
        "OTHER" => PaymentMethod.Other,
        _ => Enum.Parse<PaymentMethod>(value, ignoreCase: true),
    };
}
