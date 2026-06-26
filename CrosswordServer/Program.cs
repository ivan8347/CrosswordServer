using CrosswordServer.Storage;   // подключаем наше хранилище игр (класс GameStorage)
using CrosswordServer.Models;    // подключаем модели JSON-запросов (CreateGameRequest, JoinGameRequest и т.д.)

// =============================================================
// Создаём объект конфигурации и DI-контейнер
// =============================================================
var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку Swagger — это инструмент для тестирования API
builder.Services.AddEndpointsApiExplorer();   // позволяет Swagger видеть наши endpoints
builder.Services.AddSwaggerGen();             // генерирует UI и документацию

// Добавляем поддержку контроллеров (на будущее, если будем расширять API)
builder.Services.AddControllers();

// =============================================================
// Создаём приложение
// =============================================================
var app = builder.Build();

// =============================================================
// Включаем Swagger только в режиме разработки
// (в продакшене можно отключить)
// =============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // включает генерацию swagger.json
    app.UseSwaggerUI();      // включает красивый UI по адресу /swagger
}

// =============================================================
// Создаём одно хранилище игр на весь сервер
// Оно живёт всё время, пока работает приложение
// =============================================================
var storage = new GameStorage();


// =============================================================
// 1) Получить список всех игр
// GET /games
// =============================================================
app.MapGet("/games", () =>
{
    // получаем все активные игры из хранилища
    var games = storage.GetAllGames();

    // преобразуем игры в удобный JSON-формат
    var result = games.Select(g => new
    {
        gameId = g.GameId,                               // уникальный ID игры
        creator = g.CreatorName,                         // имя создателя
        players = g.Players.Select(p => p.PlayerName),   // список игроков
        status = g.Status.ToString(),                    // статус игры (Waiting, Playing, Finished)
        difficulty = g.Difficulty                        // сложность
    });

    // отправляем клиенту JSON
    return Results.Ok(result);
});


// =============================================================
// 2) Создать игру
// POST /game/create
// Принимаем JSON: { creatorName, difficulty }
// =============================================================
app.MapPost("/game/create", (CreateGameRequest req) =>
{
    // создаём игру в хранилище
    var game = storage.CreateGame(req.CreatorName, req.Difficulty);

    // отправляем клиенту только нужные данные
    return Results.Ok(new
    {
        gameId = game.GameId,          // ID игры
        seed = game.Seed,              // seed для генерации кроссворда
        creator = game.CreatorName,    // имя создателя
        difficulty = game.Difficulty,  // сложность
        status = game.Status.ToString()
    });
});


// =============================================================
// 3) Подключиться к игре
// POST /game/join
// Принимаем JSON: { gameId, playerName }
// =============================================================
app.MapPost("/game/join", (JoinGameRequest req) =>
{
    // пытаемся подключить игрока
    // метод возвращает true/false
    var ok = storage.JoinGame(req.GameId, req.PlayerName);

    // если игра не найдена — возвращаем 404
    if (!ok)
        return Results.NotFound("Игра не найдена");

    // получаем обновлённую игру
    var g = storage.GetGame(req.GameId);

    // отправляем клиенту обновлённую информацию
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
// 4) Отправить результат игрока
// POST /game/result
// Принимаем JSON: { gameId, playerName, score, time }
// =============================================================
app.MapPost("/game/result", (ResultRequest req) =>
{
    // сохраняем результат игрока
    // метод возвращает true/false
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);

    // если игра или игрок не найдены
    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    // после SubmitResult игра может быть удалена (если все игроки закончили)
    var g = storage.GetGame(req.GameId);

    // если игра удалена — сообщаем клиенту
    if (g == null)
    {
        return Results.Ok(new
        {
            deleted = true
        });
    }

    // игра ещё существует — отправляем список игроков и их результаты
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
// Подключаем контроллеры (на будущее)

app.MapControllers();

// Тестовый endpoint для проверки работы сервера
// GET /ping

app.MapGet("/ping", () =>
{
    return Results.Ok("pong");
});

// Запускаем сервер
app.Run();
