using StudentApi.Models;

namespace StudentApi.Services;

/// <summary>
/// In-memory implementation of <see cref="IStudentBillingService"/>.
///
/// The data store is a <c>static</c> list on purpose: ASP.NET Core creates a new
/// controller per request, so an instance field would be re-seeded constantly and
/// the payment written by one request would be gone by the next. A static list is
/// created once per process and is shared by every request, which makes it behave
/// like a (very small, non-durable) database: writes are visible to later reads and
/// everything resets when the app restarts.
///
/// Registered as a singleton in Program.cs, so all requests share one service instance.
/// </summary>
public class StudentBillingService : IStudentBillingService
{
    /// <summary>
    /// The mock data store. Static = one copy per process, shared across all requests.
    /// </summary>
    private static readonly List<Installment> Installments = SeedInstallments();

    /// <summary>
    /// Guards <see cref="Installments"/>. ASP.NET Core handles requests on multiple
    /// threads at the same time, so without this a GET could enumerate the list while
    /// a PUT is mutating it (which throws "Collection was modified").
    /// </summary>
    private static readonly object SyncRoot = new();

    /// <summary>
    /// The fake data we test against. Two students so both the happy path and the
    /// 404 path can be exercised without any database.
    /// </summary>
    private static List<Installment> SeedInstallments()
    {
        return new List<Installment>
        {
            // STU123 - one settled instalment plus three still outstanding.
            new Installment
            {
                StudentNo = "STU123",
                InstallmentNo = 1,
                Amount = 2500.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Paid,
                PaidDate = new DateTime(2026, 02, 15)
            },
            new Installment
            {
                StudentNo = "STU123",
                InstallmentNo = 2,
                Amount = 2500.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Pending,
                PaidDate = null
            },
            new Installment
            {
                StudentNo = "STU123",
                InstallmentNo = 3,
                Amount = 2750.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Pending,
                PaidDate = null
            },
            new Installment
            {
                StudentNo = "STU123",
                InstallmentNo = 4,
                Amount = 2750.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Pending,
                PaidDate = null
            },

            // STU456 - a second student, to prove the filter actually filters.
            new Installment
            {
                StudentNo = "STU456",
                InstallmentNo = 1,
                Amount = 1800.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Pending,
                PaidDate = null
            },
            new Installment
            {
                StudentNo = "STU456",
                InstallmentNo = 2,
                Amount = 1800.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Pending,
                PaidDate = null
            }
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<Installment>? GetInstallmentsByStudentNo(string studentNo)
    {
        // Treat a missing/blank student number as "nothing on record" so the caller
        // reports 404 rather than leaking a NullReferenceException.
        if (string.IsNullOrWhiteSpace(studentNo))
        {
            return null;
        }

        lock (SyncRoot)
        {
            var installments = Installments
                .Where(i => string.Equals(i.StudentNo, studentNo.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.InstallmentNo)
                .ToList();

            // No rows for this student == unknown student (an empty list would
            // also mean that, but null distinguishes "not found" from "found none").
            return installments.Count == 0 ? null : installments;
        }
    }

    /// <inheritdoc />
    public PayInstallmentResult PayInstallment(string studentNo, int installmentNo)
    {
        if (string.IsNullOrWhiteSpace(studentNo))
        {
            return PayInstallmentResult.StudentNotFound;
        }

        lock (SyncRoot)
        {
            var studentInstallments = Installments
                .Where(i => string.Equals(i.StudentNo, studentNo.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (studentInstallments.Count == 0)
            {
                return PayInstallmentResult.StudentNotFound;
            }

            var installment = studentInstallments
                .FirstOrDefault(i => i.InstallmentNo == installmentNo);

            if (installment is null)
            {
                return PayInstallmentResult.InstallmentNotFound;
            }

            // Idempotency guard: paying twice must not move the original PaidDate.
            if (string.Equals(installment.Status, InstallmentStatus.Paid, StringComparison.OrdinalIgnoreCase))
            {
                return PayInstallmentResult.AlreadyPaid;
            }

            // Installment is a reference type, so these assignments update the object
            // that is already sitting in the static list - no need to write it back.
            installment.Status = InstallmentStatus.Paid;
            installment.PaidDate = DateTime.Today;

            return PayInstallmentResult.Success;
        }
    }
}
