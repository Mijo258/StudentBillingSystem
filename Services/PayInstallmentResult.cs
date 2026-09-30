namespace StudentApi.Services;

/// <summary>
/// The possible outcomes of <see cref="IStudentBillingService.PayInstallment"/>.
/// Returning an outcome (rather than a bool or an exception) lets the service
/// describe *why* a payment failed, and leaves the HTTP translation to the controller.
/// </summary>
public enum PayInstallmentResult
{
    /// <summary>The instalment was found, was pending, and has now been marked as paid.</summary>
    Success = 0,

    /// <summary>No instalments exist for the supplied student number.</summary>
    StudentNotFound = 1,

    /// <summary>The student exists but has no instalment with the supplied number.</summary>
    InstallmentNotFound = 2,

    /// <summary>The instalment was already paid, so it was left untouched.</summary>
    AlreadyPaid = 3
}
