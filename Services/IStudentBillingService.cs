using StudentApi.Models;

namespace StudentApi.Services;

/// <summary>
/// Billing operations for student instalments.
/// Consumed by the controller via dependency injection; the controller never
/// touches the data store directly.
/// </summary>
public interface IStudentBillingService
{
    /// <summary>
    /// Returns every instalment belonging to the given student, ordered by
    /// instalment number.
    /// </summary>
    /// <param name="studentNo">The student number to look up (e.g. "STU123").</param>
    /// <returns>
    /// The student's instalments, or <c>null</c> when the student has no
    /// instalments on record (which the controller surfaces as 404).
    /// </returns>
    IReadOnlyList<Installment>? GetInstallmentsByStudentNo(string studentNo);

    /// <summary>
    /// Marks a single instalment as paid and stamps it with today's date.
    /// </summary>
    /// <param name="studentNo">The student number that owns the instalment.</param>
    /// <param name="installmentNo">The instalment number to settle.</param>
    /// <returns>An outcome describing what happened, which the controller maps to a status code.</returns>
    PayInstallmentResult PayInstallment(string studentNo, int installmentNo);
}
