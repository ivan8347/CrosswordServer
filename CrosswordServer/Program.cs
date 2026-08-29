using CrosswordServer.Storage;
using CrosswordServer.Models;

var builder = WebApplication.CreateBuilder(args);

// ОБЯЗАТЕЛЬНО ДЛЯ RENDER
builder.Host.ConfigureHostOptions(options =>
{
    //options.DisableFileSystemWatcher = true;
});

// Минимальные эндпоинты — без контроллеров
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Хранилище игр
builder.Services.AddSingleton<GameStorage>();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5270";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

// Swagger только в Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Маршрут проверки
app.MapGet("/", () => "Server is running!");

// Хранилище
var storage = app.Services.GetRequiredService<GameStorage>();

// ⭐ ЗАГРУЗКА ГЛОБАЛЬНОГО РЕЙТИНГА
//storage.LoadGlobalScores();
//Console.WriteLine("[SERVER] Global rating loaded.");

// ==========================
// ЭНДПОИНТЫ
// ==========================

app.MapGet("/games", () =>
{
    var games = storage.GetAllGames();
    return Results.Ok(games.Select(g => new
    {
        gameId = g.GameId,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName),
        status = g.Status.ToString(),
        difficulty = g.Difficulty
    }));
});

app.MapPost("/game/create", (CreateGameRequest req) =>
{
    var game = storage.CreateGame(req.CreatorName, req.Difficulty);
    return Results.Ok(new
    {
        gameId = game.GameId,
        seed = game.Seed,
        creator = game.CreatorName,
        difficulty = game.Difficulty,
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
        gameId = g.GameId,
        seed = g.Seed,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName).ToList(),
        status = g.Status.ToString()
    });
});

app.MapGet("/game/status/{id}", (string id) =>
{
    var game = storage.GetGame(id);
    if (game == null)
        return Results.NotFound("Игра не найдена");

    return Results.Ok(new
    {
        isCompleted = (game.Status == GameStatus.Finished)
    });
});

// ==========================
// ОТПРАВКА РЕЗУЛЬТАТА
// ==========================

app.MapPost("/game/result", (ResultRequest req) =>
{
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);
    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);

    if (g == null)
        return Results.Ok(new { deleted = true });

    // ⭐ Добавляем в глобальный рейтинг
    storage.GlobalScores.Add(new GameStorage.ScoreRecord
    {
        PlayerName = req.PlayerName,
        Score = req.Score,
        TimeSeconds = req.Time,
        Difficulty = g.Difficulty,
        Date = DateTime.UtcNow
    });

    // ⭐ Сохраняем рейтинг
    //storage.SaveGlobalScores();

    // ⭐ Проверяем завершение всех игроков
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
        players = g.Players.Select(p => new
        {
            name = p.PlayerName,
            score = p.Score,
            time = p.TimeSeconds,
            timeFormatted = p.TimeFormatted
        }).ToList()
    });
});

// ==========================
// ГЛОБАЛЬНЫЙ РЕЙТИНГ
// ==========================

app.MapGet("/rating", () =>
{
    return Results.Ok(storage.GlobalScores
        .OrderByDescending(s => s.Score)
        .ThenBy(s => s.TimeSeconds)
        .Select(s => new
        {
            s.PlayerName,
            s.Score,
            //s.TimeSeconds,
            //s.TimeFormatted,
            time = $"{s.TimeSeconds / 60:D2}:{s.TimeSeconds % 60:D2}",
            s.Difficulty,
            s.Date
        }));
});

// ==========================
// РЕЗУЛЬТАТЫ ИГРЫ
// ==========================

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
            timeSeconds = p.TimeSeconds,
            timeFormatted = p.TimeFormatted
        })
        .ToList();

    game.ResultsRequestsCount++;

    if (game.ResultsRequestsCount >= game.Players.Count)
        storage.DeleteGame(id);

    return Results.Ok(results);
});

// ==========================
// ЧАТ
// ==========================

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

// ==========================
// PING
// ==========================

app.MapGet("/ping", () => Results.Ok("pong"));

app.Run();
