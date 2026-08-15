using CrosswordServer.Storage;
using CrosswordServer.Models;

var builder = WebApplication.CreateBuilder(args);

// Отключаем FileSystemWatcher — обязательно для Render
builder.Host.ConfigureHostOptions(options =>
{
    //options.DisableFileSystemWatcher = true;
});

// Добавляем поддержку Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Добавляем контроллеры
builder.Services.AddControllers();

// Добавляем хранилище игр как Singleton
builder.Services.AddSingleton<GameStorage>();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5270";

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(int.Parse(port));
//});

builder.WebHost.UseSetting("dotnet-hot-reload", "false");

var app = builder.Build();

// Включаем Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// Маршрут для проверки
app.MapGet("/", () => "Server is running!");

// Подключаем контроллеры
app.MapControllers();

// ==========================
// ТВОИ ЭНДПОИНТЫ
// ==========================

// Хранилище игр
var storage = app.Services.GetRequiredService<GameStorage>();

// 1) Получить список всех игр
app.MapGet("/games", () =>
{
    var games = storage.GetAllGames();
    var result = games.Select(g => new
    {
        gameId = g.GameId,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName),
        status = g.Status.ToString(),
        difficulty = g.Difficulty
    });
    return Results.Ok(result);
});

// 2) Создать игру
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

// 3) Подключиться к игре
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
        players = g.Players.Select(p => p.PlayerName).ToList(),
        status = g.Status.ToString()
    });
});
// 6) Получить статус игры
/*app.MapGet("/game/status/{id}", (string id) =>
{
    var game = storage.GetGame(id);
    if (game == null)
        return Results.NotFound(new { error = "Game not found" });

    return Results.Ok(new
    {
        gameId = game.GameId,
        status = game.Status.ToString(),
        difficulty = game.Difficulty,
        creator = game.CreatorName,
        playerCount = game.Players.Count,
        isFull = game.Players.Count >=2 // пример логики
    });
});*/
app.MapGet("/game/status/{id}", (string id) =>
{
    var game = storage.GetGame(id);
    if (game == null)
        return Results.NotFound("Игра не найдена");
    // Возвращаем простой JSON: {"isCompleted": true/false}
    return Results.Ok(new
    {
        isCompleted = (game.Status == GameStatus.Finished)
    });
});

// 4) Отправить результат игрока + авто‑удаление игры
app.MapPost("/game/result", (ResultRequest req) =>
{
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);
    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);
    if (g == null)
        return Results.Ok(new { deleted = true });

    bool allPlayersReported = g.Players.All(p => p.Score > 0 || p.TimeSeconds > 0);

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
            time = p.TimeSeconds
        }).ToList()
    });
});

// 5) Получить результаты игры
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

    var response = Results.Ok(results);

    game.ResultsRequestsCount++;
    if (game.ResultsRequestsCount >= game.Players.Count)
        storage.DeleteGame(id);

    return response;
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

// Ping
app.MapGet("/ping", () => Results.Ok("pong"));

// Запуск сервера
app.Run();
