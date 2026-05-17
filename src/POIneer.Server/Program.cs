using POIneer.Server.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

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