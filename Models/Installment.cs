namespace StudentApi.Models;

/// <summary>
/// A single instalment owed by a student.
/// This is the entity that is held in memory by the billing service and
/// returned to the client. In a real system it would be mapped to/from a
/// database table and hidden behind a DTO.
/// </summary>
public class Installment
{
    /// <summary>The student this instalment belongs to (e.g. "STU123").</summary>
    public string StudentNo { get; set; } = string.Empty;

    /// <summary>Sequential instalment number for the student (1-based).</summary>
    public int InstallmentNo { get; set; }

    /// <summary>Amount due for this instalment.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO currency code the amount is expressed in (e.g. "ZAR").</summary>
    public string Currency { get; set; } = "ZAR";

    /// <summary>
    /// Current state of the instalment. One of <see cref="InstallmentStatus.Pending"/>
    /// or <see cref="InstallmentStatus.Paid"/>.
    /// </summary>
    public string Status { get; set; } = InstallmentStatus.Pending;

    /// <summary>The date the instalment was settled; <c>null</c> while it is still pending.</summary>
    public DateTime? PaidDate { get; set; }
}
