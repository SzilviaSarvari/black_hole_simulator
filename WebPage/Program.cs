using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapPost("/start", () =>
{
    Process.Start(@"C:\Users\User\Documents\Egyetem\Rendszertesztelés\Gyakorlas\black_hole_simulator\BlackHoleGraphic\bin\Debug\net9.0\BlackHoleGraphic.exe");
    return Results.Ok();
});

app.Run("http://localhost:5000");