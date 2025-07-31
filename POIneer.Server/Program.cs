using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POIneer.Server.Services;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

var processor = new OsmProcessingService();
//processor.GenerateDummySQLite("Data/berlin.sqlite");

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
