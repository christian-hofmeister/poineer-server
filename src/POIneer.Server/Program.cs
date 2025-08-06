using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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



var processor = new OsmProcessingService();
//processor.GenerateDummySQLite("Data/berlin.sqlite");

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
