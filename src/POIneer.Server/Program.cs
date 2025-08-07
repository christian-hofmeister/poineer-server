using POIneer.Server.Services;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "POIneer API",
        Version = "v1",
        Description = "POI download and region management API for the POIneer mobile app"
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "POIneer API v1");
        options.RoutePrefix = ""; // Swagger UI unter root (/)
    });
}

if (!app.Environment.IsEnvironment("Testing"))
{
    Console.WriteLine($"Current environment: {app.Environment.EnvironmentName}");

    app.UseHttpsRedirection();
}



var processor = new OsmProcessingService();
//processor.GenerateDummySQLite("Data/berlin.sqlite");


app.MapControllers();
app.MapGet("/health", () => Results.Ok("API is running"));

app.Run();

// This partial class allows the Program class to be referenced in tests
// without needing to change the namespace or structure of the main application code.
namespace POIneer.Server
{
    public partial class Program { }
}
