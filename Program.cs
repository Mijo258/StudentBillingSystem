using Microsoft.OpenApi.Models;
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

// Register the billing service so the controller can receive it via constructor injection.
// Singleton: the mock data lives in a static list, so one shared instance is all we need,
// and the payments written during the app's lifetime stay visible to later requests.
builder.Services.AddSingleton<IStudentBillingService, StudentBillingService>();

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
