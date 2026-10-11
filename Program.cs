using Microsoft.OpenApi.Models;
using StudentApi.Data;
using StudentApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers(); // We need this to handle our Student API requests
builder.Services.AddEndpointsApiExplorer();

// Name the document "v1" explicitly. The generated file then always lands at
// /swagger/v1/swagger.json - exactly the URL the UI below asks for.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Billing API",
        Version = "v1",
        Description = "Fetch a student's instalments and mark individual instalments as paid."
    });
});

// Register the database context. Which database we run on, and its connection string, are
// read from configuration inside Data/DatabaseSetup.cs - this file deliberately does not name
// a database anywhere, which is what makes the provider swappable.
//
// AddDbContext gives StudentDbContext a SCOPED lifetime: ASP.NET Core creates one instance per
// HTTP request and disposes it when the request ends.
builder.Services.AddStudentDatabase(builder.Configuration);

// Register the billing service so the controller can receive it via constructor injection.
//
// This MUST be Scoped (or Transient), not Singleton. A singleton is created once and lives
// for the whole process, so it can never legally hold a per-request DbContext - that is the
// "captive dependency" problem, and ASP.NET Core refuses to start rather than let it slide:
//     InvalidOperationException: Cannot consume scoped service 'StudentDbContext'
//     from singleton 'IStudentBillingService'.
// Matching the context's lifetime is both correct and the simplest rule to remember.
builder.Services.AddScoped<IStudentBillingService, StudentBillingService>();

var app = builder.Build();

// 2. Configure the HTTP request pipeline.


    // UseSwagger() serves the JSON document, UseSwaggerUI() serves the page that reads it.
    // Both are required - the UI on its own has nothing to display.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Use an ABSOLUTE path ("/swagger/..."), not a relative one ("./v1/swagger.json").
        // A relative path is resolved against whatever URL the browser is sitting on, so it
        // only works when that URL ends in a slash:
        //     /swagger/  -> ./v1/swagger.json -> /swagger/v1/swagger.json   (OK)
        //     /swagger   -> ./v1/swagger.json -> /v1/swagger.json            (404!)
        // The absolute path works from either, which is why "add a dot" fixes it for some
        // people (they were on the trailing-slash URL) and not for others.
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Billing API v1");
    });

    // Nothing is mapped at "/", so browsing to the root used to 404.
    // Point it at the Swagger UI instead.
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));


// FIX: We comment this out to stop the HTTPS redirect error you were getting!
// app.UseHttpsRedirection();

app.UseAuthorization();

// 3. Map the controllers
app.MapControllers();

app.Run();
