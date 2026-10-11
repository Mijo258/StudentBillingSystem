using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StudentApi.Data;

/// <summary>
/// The single place in the application that knows which database we run on.
///
/// Everything else - the controller, the service, the models, and even
/// <see cref="StudentDbContext"/> itself - is plain EF Core and names no database at all.
/// That is what makes the provider swappable: the decision is made here, once, at startup,
/// and nowhere else. The business code depends on <see cref="StudentDbContext"/>; only this
/// file depends on Oracle.
///
/// To change database you change exactly four things, and none of them are the service or
/// the controller:
///   1. this file      - add a case to the switch below,
///   2. the .csproj    - add that database's EF Core provider package,
///   3. appsettings.json - set "Database": { "Provider": "..." },
///   4. Migrations/    - regenerate them (see the note in the switch below).
/// </summary>
public static class DatabaseSetup
{
    /// <summary>Configuration key holding the provider name, e.g. "Oracle".</summary>
    public const string ProviderConfigKey = "Database:Provider";

    /// <summary>Name of the entry under "ConnectionStrings" that holds our connection string.</summary>
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>The provider this application currently supports.</summary>
    public const string Oracle = "Oracle";

    /// <summary>
    /// Registers <see cref="StudentDbContext"/> using the provider named in configuration.
    /// Called from Program.cs with a single line, which is the whole point: Program.cs does
    /// not need to know the answer to "which database?".
    /// </summary>
    public static IServiceCollection AddStudentDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // "?? Oracle" keeps the app working if the key is missing entirely, so an
        // appsettings.json from an older version cannot break startup. Trim() forgives a
        // stray space - a very easy thing to leave in a JSON value.
        var provider = (configuration[ProviderConfigKey] ?? Oracle).Trim();

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        // Fail loudly and early rather than letting a null connection string travel all the
        // way to the first query, where it would surface as a confusing driver error.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string configured under 'ConnectionStrings:{ConnectionStringName}'.");
        }

        // AddDbContext is what gives StudentDbContext its SCOPED lifetime: ASP.NET Core builds
        // one instance per HTTP request and disposes it when the request ends. That lifetime is
        // also why IStudentBillingService must be Scoped and not Singleton (see Program.cs).
        return services.AddDbContext<StudentDbContext>(options =>
        {
            switch (provider)
            {
                // UseOracle tells EF Core to speak Oracle's dialect: how to generate SQL, which
                // column types to use, how to bind parameters. Swapping this single call is the
                // code half of changing database.
                case Oracle:
                    options.UseOracle(connectionString);
                    break;

                // ---- Adding another database goes HERE ----
                //   1. dotnet add package Microsoft.EntityFrameworkCore.Sqlite     (for example)
                //   2. add:  case "Sqlite": options.UseSqlite(connectionString); break;
                //   3. set  "Database": { "Provider": "Sqlite" }  in appsettings.json
                //   4. regenerate migrations - delete the Migrations/ folder, then:
                //          dotnet ef migrations add InitialCreate
                //          dotnet ef database update
                //   5. that is all. The service, the interface, the controller and the models
                //      are not touched, because none of them name a database.
                //
                // Why step 4 is unavoidable: a migration is written as provider-specific SQL.
                // Ours contains NVARCHAR2, NUMBER(10) and TIMESTAMP(7), which are Oracle types
                // and do not exist in SQL Server or SQLite. Migrations are therefore NOT
                // portable between databases and must be regenerated for each one.
                //
                // Note: different providers want different connection string shapes. SQLite
                // takes a file path - "Data Source=studentbilling.db" - but it still goes under
                // the same ConnectionStrings:DefaultConnection key, so nothing else changes.

                default:
                    // An unknown name is a configuration mistake. Throwing here means the app
                    // refuses to start with a message that says exactly what is wrong, instead
                    // of starting up and failing on the first request.
                    throw new InvalidOperationException(
                        $"Unsupported database provider '{provider}' in '{ProviderConfigKey}'. " +
                        $"Supported values: {Oracle}.");
            }
        });
    }
}
