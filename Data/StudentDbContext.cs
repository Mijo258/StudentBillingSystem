using Microsoft.EntityFrameworkCore;
using StudentApi.Models;

namespace StudentApi.Data;

/// <summary>
/// The EF Core gateway to the Oracle database.
///
/// A DbContext represents one unit of work: it tracks the entities you load, remembers
/// what you changed, and turns <see cref="SaveChanges"/> into the corresponding SQL. It is
/// registered in Program.cs with AddDbContext, which gives it a *scoped* lifetime - one
/// instance per HTTP request - and that lifetime is the reason the billing service had to
/// stop being a singleton (see Program.cs).
/// </summary>
public class StudentDbContext : DbContext
{
    /// <summary>
    /// The options (provider + connection string) are supplied by the DI container, which
    /// builds them in Program.cs. This constructor is what lets the context be injected.
    /// </summary>
    public StudentDbContext(DbContextOptions<StudentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Represents the INSTALLMENTS table. Every query starts here: _context.Installments...
    /// </summary>
    public DbSet<Installment> Installments { get; set; } = null!;

    /// <summary>
    /// Configures the mapping between the <see cref="Installment"/> class and the database
    /// table. EF Core can infer a lot by convention, but the parts that matter are spelled
    /// out here so they are explicit rather than lucky.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var installment = modelBuilder.Entity<Installment>();

        // ---- Primary key: COMPOSITE (StudentNo + InstallmentNo) ----
        // This is not just the tidier option, it is the only correct one. InstallmentNo is
        // only unique *within* a student: STU123 has an instalment 1 and so does STU456. If
        // InstallmentNo were the key on its own, the second student's instalment 1 would
        // collide with the first student's and the insert would fail. A composite key says
        // exactly what the data means: "this student's Nth instalment".
        installment.HasKey(i => new { i.StudentNo, i.InstallmentNo });

        // InstallmentNo is a meaningful business value (1, 2, 3...), not an auto-generated
        // surrogate id, so tell EF never to try to generate it. Without this, EF can treat an
        // integer key part as an identity column and fight you on inserts.
        installment.Property(i => i.InstallmentNo).ValueGeneratedNever();

        installment.Property(i => i.StudentNo)
            .HasMaxLength(20)
            .IsRequired();

        // decimal has no natural SQL precision: without this, EF warns and Oracle may default
        // to a scale that silently rounds money. NUMBER(18,2) is the safe money column.
        installment.Property(i => i.Amount)
            .HasPrecision(18, 2);

        installment.Property(i => i.Currency)
            .HasMaxLength(3)
            .IsRequired();

        installment.Property(i => i.Status)
            .HasMaxLength(20)
            .IsRequired();

        // PaidDate stays nullable: it is null exactly while the instalment is unpaid, which is
        // what makes it a useful column rather than a flag we have to keep in sync.

        // ---- Seed data ----
        // The old service seeded these rows in a static list. Now that the list is gone, this
        // is where they live: HasData writes them into the migration, so `database update`
        // inserts them and the API is testable immediately instead of returning 404 for
        // everything. Values must be CONSTANTS (no DateTime.Now) or EF cannot tell whether the
        // model changed between migrations.
        installment.HasData(
            new Installment
            {
                StudentNo = "STU123",
                InstallmentNo = 1,
                Amount = 2500.00m,
                Currency = "ZAR",
                Status = InstallmentStatus.Paid,
                PaidDate = new DateTime(2026, 2, 15)
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

            // STU456 - a second student, so the filter can be proven to filter.
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
            });
    }
}
