using StudentApi.Data;
using StudentApi.Models;

namespace StudentApi.Services;

/// <summary>
/// Database-backed implementation of <see cref="IStudentBillingService"/>.
///
/// This used to hold a static in-memory list; it now talks to Oracle through
/// <see cref="StudentDbContext"/>. Two things changed as a direct consequence:
///
///   1. The data is now durable. It used to reset on every restart.
///   2. The old <c>lock (SyncRoot)</c> is gone. That lock existed only because the static
///      list was shared by every request at once. A DbContext is not shared - it is scoped
///      to a single request - so there is nothing left to guard here. (Concurrency between
///      requests is now the database's problem, not this class's.)
/// </summary>
public class StudentBillingService : IStudentBillingService
{
    private readonly StudentDbContext _context;

    /// <summary>
    /// The context is supplied by the DI container, which builds it per request and hands it
    /// the connection string configured in Program.cs.
    /// </summary>
    public StudentBillingService(StudentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public IReadOnlyList<Installment>? GetInstallmentsByStudentNo(string studentNo)
    {
        // Same contract as before: a missing/blank student number means "nothing on record",
        // so the controller reports 404 rather than leaking an exception.
        if (string.IsNullOrWhiteSpace(studentNo))
        {
            return null;
        }

        // ToUpper() on BOTH sides keeps the case-insensitive matching the in-memory version had.
        // This matters: the old code used
        //     string.Equals(i.StudentNo, x, StringComparison.OrdinalIgnoreCase)
        // and EF Core CANNOT translate that overload into SQL - it throws
        // "could not be translated" at runtime. UPPER(col) = UPPER(@p) is real SQL and Oracle
        // runs it happily.
        var normalized = studentNo.Trim().ToUpper();

        var installments = _context.Installments
            .Where(i => i.StudentNo.ToUpper() == normalized)
            .OrderBy(i => i.InstallmentNo)
            .ToList();

        // No rows for this student == unknown student (an empty list would mean the same, but
        // null distinguishes "not found" from "found none").
        return installments.Count == 0 ? null : installments;
    }

    /// <inheritdoc />
    public PayInstallmentResult PayInstallment(string studentNo, int installmentNo)
    {
        if (string.IsNullOrWhiteSpace(studentNo))
        {
            return PayInstallmentResult.StudentNotFound;
        }

        var normalized = studentNo.Trim().ToUpper();

        // DELIBERATELY one query, in the same shape as the GET endpoint's - a filter plus a
        // materialisation - because that shape is known to work on this database.
        //
        // The previous version asked the database two questions (.Any(...) and then
        // .FirstOrDefault(...)). .Any(...) makes EF Core produce a *boolean value* rather than
        // a comparison, and materialising a boolean is exactly what this Oracle version cannot
        // do: it has no TRUE/FALSE literal in SQL (that arrived in Oracle 23c), so the generated
        // query died with ORA-00904: "FALSE": invalid identifier.
        //
        // A predicate inside .Where(...) compiles to a comparison (UPPER(col) = :p1), which every
        // Oracle understands. So: fetch the student's rows once, then answer both of our questions
        // in C# from the list we already have. A student has a handful of instalments, so this is
        // a cheap trade - and it works regardless of Oracle version.
        var studentInstallments = _context.Installments
            .Where(i => i.StudentNo.ToUpper() == normalized)
            .ToList();

        if (studentInstallments.Count == 0)
        {
            return PayInstallmentResult.StudentNotFound;
        }

        // Enumerable.FirstOrDefault on a List - this runs in C#, not in SQL, so there is no
        // translation involved and no boolean to materialise. The object returned is the same
        // instance the DbContext is tracking, which is what makes the save below work.
        var installment = studentInstallments
            .FirstOrDefault(i => i.InstallmentNo == installmentNo);

        if (installment is null)
        {
            return PayInstallmentResult.InstallmentNotFound;
        }

        // Idempotency guard: paying twice must not move the original PaidDate.
        // This comparison runs in C# on a tracked object, not in SQL, so StringComparison is
        // fine here - translation limits only apply to expressions inside the LINQ query.
        if (string.Equals(installment.Status, InstallmentStatus.Paid, StringComparison.OrdinalIgnoreCase))
        {
            return PayInstallmentResult.AlreadyPaid;
        }

        // Because the entity is tracked, these are just property assignments on a C# object.
        // Nothing is written to the database yet.
        installment.Status = InstallmentStatus.Paid;

        // DateTime.UtcNow rather than DateTime.Today: a database column should store an
        // unambiguous instant. Today's date is whatever the server's local timezone says it is,
        // which stops being obvious the moment the app and the database are in different places.
        installment.PaidDate = DateTime.UtcNow;

        // This is the line that actually issues the UPDATE. Until SaveChanges() is called, the
        // change exists only in memory - which is exactly why the whole unit of work can be
        // committed or abandoned as one.
        _context.SaveChanges();

        return PayInstallmentResult.Success;
    }
}
