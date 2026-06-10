using POIneer.Server.Api.Endpoints;
using POIneer.Server.Application.Abstractions;
using POIneer.Server.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDatasetMetadataProvider, HardcodedDatasetMetadataProvider>();

var app = builder.Build();
app.MapHealthEndpoints();

// Configure the HTTP request pipeline.
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    //
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();