using CrosswordServer.Storage;   // подключаем наше хранилище игр
using CrosswordServer.Models;    // подключаем модели JSON-запросов

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// создаЄм одно хранилище игр на весь сервер
// оно живЄт всЄ врем€, пока работает приложение
var storage = new GameStorage();


// =============================================================
// 1) ѕолучить список всех игр
// =============================================================
app.MapGet("/games", () =>
{
    // получаем все активные игры из хранилища
    var games = storage.GetAllGames();

    // преобразуем игры в удобный JSON-формат
    var result = games.Select(g => new
    {
        gameId = g.GameId,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName).ToList(),
        status = g.Status.ToString(),
        difficulty = g.Difficulty
    });

    // отправл€ем клиенту JSON
    return Results.Ok(result);
});


// =============================================================
// 2) —оздать игру (принимаем JSON)
// =============================================================
app.MapPost("/game/create", (CreateGameRequest req) =>
{
    // создаЄм игру в хранилище
    var game = storage.CreateGame(req.CreatorName, req.Difficulty);

    // отправл€ем клиенту только нужные данные
    return Results.Ok(new
    {
        gameId = game.GameId,
        seed = game.Seed,
        creator = game.CreatorName,
        difficulty = game.Difficulty,
        status = game.Status.ToString()
    });
});


// =============================================================
// 3) ѕодключитьс€ к игре (принимаем JSON)
// =============================================================
app.MapPost("/game/join", (JoinGameRequest req) =>
{
    // пытаемс€ подключить игрока
    // метод возвращает true/false
    var ok = storage.JoinGame(req.GameId, req.PlayerName);

    // если игра не найдена Ч возвращаем 404
    if (!ok)
        return Results.NotFound("»гра не найдена");

    // получаем обновлЄнную игру
    var g = storage.GetGame(req.GameId);

    // отправл€ем клиенту обновлЄнную информацию
    return Results.Ok(new
    {
        gameId = g.GameId,
        seed = g.Seed,
        creator = g.CreatorName,
        players = g.Players.Select(p => p.PlayerName).ToList(),
        status = g.Status.ToString()
    });
});


// =============================================================
// 4) ќтправить результат игрока (принимаем JSON)
// =============================================================
app.MapPost("/game/result", (ResultRequest req) =>
{
    // сохран€ем результат игрока
    // метод возвращает true/false
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);

    // если игра или игрок не найдены
    if (!ok)
        return Results.NotFound("»гра не найдена или игрок отсутствует");

    // после SubmitResult игра может быть удалена (если все игроки закончили)
    var g = storage.GetGame(req.GameId);

    // если игра удалена Ч сообщаем клиенту
    if (g == null)
    {
        return Results.Ok(new
        {
            deleted = true
        });
    }

    // игра ещЄ существует Ч отправл€ем список игроков и их результаты
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


// запускаем сервер
app.Run();
