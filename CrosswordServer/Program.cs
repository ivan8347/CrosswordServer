using CrosswordServer.Storage;
using CrosswordServer.Models;

var builder = WebApplication.CreateBuilder(args);

// Отключаем мониторинг файлов — обязательно для Render (чтобы не было inotify error)
foreach (var provider in builder.Configuration.Providers)
{
    if (provider is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jsonProvider)
    {
        jsonProvider.ReloadOnChange = false;
    }
}

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

// Список всех игр — сложность уже есть
app.MapGet("/games", () =>
{
    var games = storage.GetAllGames();
    return Results.Ok(games.Select(g => new
    {
        gameId = g.GameId,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName),
        status = g.Status.ToString(),
        difficulty = g.Difficulty          // ✅ сложность передаётся
    }));
});

// Создание игры — сложность уже есть
app.MapPost("/game/create", (CreateGameRequest req) =>
{
    var game = storage.CreateGame(req.CreatorName, req.Difficulty);
    return Results.Ok(new
    {
        gameId = game.GameId,
        seed = game.Seed,
        creator = game.CreatorName,
        difficulty = game.Difficulty,      // ✅ сложность передаётся
        status = game.Status.ToString()
    });
});

// Подключение к игре — ДОБАВЛЕНО difficulty
app.MapPost("/game/join", (JoinGameRequest req) =>
{
    var ok = storage.JoinGame(req.GameId, req.PlayerName);
    if (!ok)
        return Results.NotFound("Игра не найдена");

    var g = storage.GetGame(req.GameId);
    return Results.Ok(new
    {
        gameId = g.GameId,
        seed = g.Seed,
        creator = g.CreatorName,
        difficulty = g.Difficulty,          // ✅ добавлено
        players = g.Players.Select(p => p.PlayerName).ToList(),
        status = g.Status.ToString()
    });
});

// Статус игры — ДОБАВЛЕНО difficulty
app.MapGet("/game/status/{id}", (string id) =>
{
    var game = storage.GetGame(id);
    if (game == null)
        return Results.NotFound("Игра не найдена");

    return Results.Ok(new
    {
        isCompleted = (game.Status == GameStatus.Finished),
        difficulty = game.Difficulty         // ✅ добавлено
    });
});

// Отправка результата — сложность не нужна в ответе, но можно добавить для удобства
app.MapPost("/game/result", (ResultRequest req) =>
{
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);
    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);
    if (g == null)
        return Results.Ok(new { deleted = true });

    bool allPlayersReported = g.Players.All(p => p.HasReported);

    if (allPlayersReported && g.Status != GameStatus.Finished)
    {
        g.Status = GameStatus.Finished;
        storage.DeleteGame(req.GameId);
    }

    return Results.Ok(new
    {
        deleted = false,
        status = g.Status.ToString(),
        difficulty = g.Difficulty,            // ✅ добавлено (опционально)
        players = g.Players.Select(p => new
        {
            name = p.PlayerName,
            score = p.Score,
            time = p.TimeSeconds
        }).ToList()
    });
});

// Результаты игры — ДОБАВЛЕНО difficulty
app.MapGet("/results/{id}", (string id) =>
{
    var game = storage.GetGame(id);
    if (game == null)
        return Results.NotFound("Игра не найдена");

    var results = game.Players
        .OrderByDescending(p => p.Score)
        .ThenBy(p => p.TimeSeconds)
        .Select(p => new
        {
            playerName = p.PlayerName,
            score = p.Score,
            timeSeconds = p.TimeSeconds
        })
        .ToList();

    game.ResultsRequestsCount++;

    if (game.ResultsRequestsCount >= game.Players.Count)
        storage.DeleteGame(id);

    return Results.Ok(new
    {
        difficulty = game.Difficulty,         // ✅ добавлено
        results = results
    });
});

// Чат
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
