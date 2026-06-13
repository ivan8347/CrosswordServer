using CrosswordServer.Storage;
using CrosswordServer.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// создаём одно хранилище игр на весь сервер
var storage = new GameStorage();

// 1) Получить список всех игр

app.MapGet("/games", () =>
{
    // Берём все активные игры
    var games = storage.GetAllGames();

    // Преобразуем в удобный формат для клиента
    var result = games.Select
    (
        g => new
        {
            gameId = g.GameId,
            creator = g.CreatorName,
            players = g.Players.Select(p => p.PlayerName).ToList(),
            status = g.Status.ToString()
        }
    );

    return Results.Ok(result);
});

app.Run();

// 2) Создать игру

app.MapPost("/game/create", (string creatorName, string difficulty) =>
{
    // создаём игру в хранилище
    var game = storage.CreateGame(creatorName, difficulty);

    // отправляем клиенту только нужные данные
    return Results.Ok
    (new
    {
        gameId = game.GameId,
        seed = game.Seed,
        creator = game.CreatorName,
        difficulty = game.Difficulty,
        status = game.Status.ToString()
    });
});

// 3) Подключиться к игре

app.MapPost("/game/join", (string gameId, string playerName) =>
{
    // пытаемся подключить игрока
    bool ok = storage.JoinGame(gameId, playerName);

    if (!ok)
        return Results.NotFound("Игра не найдена");

    // получаем игру после подключения
    var g = storage.GetGame(gameId);

    // отправляем клиенту обновлённую информацию об игре
    return Results.Ok(new
    {
        gameId = g.GameId,
        seed = g.Seed,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName).ToList(),
        status = g.Status.ToString()
    });
});

// 4) Отправить результат игрока

app.MapPost("/game/result", (string gameId, string playerName, int score, int time) =>
{
    // пытаемся сохранить результат
    bool ok = storage.SubmitResult(gameId, playerName, score, time);

    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    // после SubmitResult игра может быть удалена
    // проверяем, осталась ли она в хранилище
    var g = storage.GetGame(gameId);

    if (g == null)
    {
        // игра завершена и удалена
        return Results.Ok(new
        {
            deleted = true
        });
    }

    // игра ещё не завершена
    return Results.Ok(new
    {
        deleted = false,
        players = g.Players.Select(p => new
        {
            name = p.PlayerName,
            score = p.Score,
            time = p.TimeSeconds
        }).ToList()
    });
});

