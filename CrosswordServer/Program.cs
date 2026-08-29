using CrosswordServer.Storage;
using CrosswordServer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(options =>
{
    //options.DisableFileSystemWatcher = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<GameStorage>();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5270";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Server is running!");

var storage = app.Services.GetRequiredService<GameStorage>();

storage.LoadGlobalScores();
Console.WriteLine("[SERVER] Global rating loaded.");

app.MapGet("/games", () =>
{
    var games = storage.GetAllGames();
    return Results.Ok(games.Select(g => new
    {
        g.GameId,
        g.CreatorName,
        players = g.Players.Select(p => p.PlayerName),
        status = g.Status.ToString(),
        g.Difficulty
    }));
});

app.MapPost("/game/create", (CreateGameRequest req) =>
{
    var game = storage.CreateGame(req.CreatorName, req.Difficulty);
    return Results.Ok(new
    {
        game.GameId,
        game.Seed,
        game.CreatorName,
        game.Difficulty,
        status = game.Status.ToString()
    });
});

app.MapPost("/game/join", (JoinGameRequest req) =>
{
    var ok = storage.JoinGame(req.GameId, req.PlayerName);
    if (!ok)
        return Results.NotFound("Игра не найдена");

    var g = storage.GetGame(req.GameId)!;
    return Results.Ok(new
    {
        g.GameId,
        g.Seed,
        g.CreatorName,
        players = g.Players.Select(p => p.PlayerName),
        status = g.Status.ToString()
    });
});

app.MapPost("/game/result", (ResultRequest req) =>
{
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);
    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);

    if (g == null)
        return Results.Ok(new { deleted = true });

    bool allPlayersReported = g.Players.All(p => p.HasReported);

    if (allPlayersReported)
    {
        g.Status = GameStatus.Finished;
        storage.DeleteGame(req.GameId);
    }

    return Results.Ok(new
    {
        deleted = false,
        status = g.Status.ToString(),
        players = g.Players.Select(p => new
        {
            p.PlayerName,
            p.Score,
            p.TimeSeconds,
            p.TimeFormatted
        })
    });
});

app.MapGet("/rating", () =>
{
    return Results.Ok(storage.GlobalScores
        .OrderByDescending(s => s.Score)
        .ThenBy(s => s.TimeSeconds)
        .Select(s => new
        {
            s.PlayerName,
            s.Score,
            s.TimeSeconds,
            s.TimeFormatted,
            s.Difficulty,
            s.Date
        }));
});

app.MapPost("/chat", (ChatMessage msg) =>
{
    msg.Time = DateTime.UtcNow;
    storage.GlobalChat.Add(msg);
    return Results.Ok();
});

app.MapGet("/chat", () =>
{
    return Results.Ok(storage.GlobalChat.OrderBy(m => m.Time));
});

app.MapGet("/ping", () => Results.Ok("pong"));

app.Run();
