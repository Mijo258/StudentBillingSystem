namespace StudentApi.Models;

/// <summary>
/// The allowed values for <see cref="Installment.Status"/>.
/// Kept as constants so the service and controller never compare against
/// hand-typed string literals.
/// </summary>
public static class InstallmentStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
}
