namespace kecas.domain.common;

/// <summary>
/// Money is stored as an integer amount of kobo (NGN minor unit), matching the
/// design decision in the system design document (amount_kobo). Never use
/// floating point for money.
/// </summary>
public readonly record struct Money
{
    public Money(int amountInKobo)
    {
        if (amountInKobo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountInKobo), "Money cannot be negative.");
        }

        AmountInKobo = amountInKobo;
    }

    public int AmountInKobo { get; }

    public static Money Zero => new(0);

    public string ToDisplayString() =>
        AmountInKobo % 100 == 0
            ? $"NGN {AmountInKobo / 100:0}"
            : $"NGN {AmountInKobo / 100.0:0.00}";

    public override string ToString() => ToDisplayString();
}
