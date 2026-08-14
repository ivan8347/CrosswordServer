using CrosswordServer.Storage;   // подключаем наше хранилище игр (класс GameStorage)
using CrosswordServer.Models;    // подключаем модели JSON-запросов (CreateGameRequest, JoinGameRequest и т.д.)

// Создаём объект конфигурации и DI-контейнер
var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку Swagger — это инструмент для тестирования API
builder.Services.AddEndpointsApiExplorer();   // позволяет Swagger видеть наши endpoints
builder.Services.AddSwaggerGen();             // генерирует UI и документацию

// Добавляем поддержку контроллеров (на будущее, если будем расширять API)
builder.Services.AddControllers();

// Создаём приложение
var app = builder.Build();

// Включаем Swagger только в режиме разработки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // включает генерацию swagger.json
    app.UseSwaggerUI();      // включает красивый UI по адресу /swagger
}

// Создаём одно хранилище игр на весь сервер
// Оно живёт всё время, пока работает приложение
var storage = new GameStorage();

// 1) Получить список всех игр
// GET /games

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


// 2) Создать игру
// POST /game/create
// Принимаем JSON: { creatorName, difficulty }
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


// 3) Подключиться к игре
// POST /game/join
// Принимаем JSON: { gameId, playerName }
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
// 4) Отправить результат игрока
/*app.MapPost("/game/result", (ResultRequest req) =>
{
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);

    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);

    if (g == null)
    {
        return Results.Ok(new { deleted = true });
    }

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
});*/
// =============================================================
// 4) Отправить результат игрока + АВТОМАТИЧЕСКОЕ ЗАВЕРШЕНИЕ
// POST /game/result
// =============================================================
/*app.MapPost("/game/result", async (ResultRequest req) =>
{
    // Пытаемся сохранить результат
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);

    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);
    if (g == null)
    {
        return Results.Ok(new { deleted = true });
    }

    // ⭐⭐ ГЛАВНАЯ ЛОГИКА: Проверяем, сдали ли результаты ВСЕ игроки ⭐⭐
    // Предполагаем, что если у всех игроков есть Score > 0 или Time > 0, значит они сдали
    bool allPlayersReported = g.Players.All(p => p.Score > 0 || p.TimeSeconds > 0);

    if (allPlayersReported && g.Status != GameStatus.Finished)
    {
        // Если все сдали — сразу помечаем игру как завершенную!
        g.Status = GameStatus.Finished;
        Console.WriteLine($"[SERVER] Игра {req.GameId} завершена: все игроки сдали результаты.");
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
});*/
app.MapPost("/game/result", (ResultRequest req) =>
{
    var ok = storage.SubmitResult(req.GameId, req.PlayerName, req.Score, req.Time);

    if (!ok)
        return Results.NotFound("Игра не найдена или игрок отсутствует");

    var g = storage.GetGame(req.GameId);
    if (g == null)
    {
        return Results.Ok(new { deleted = true });
    }

    // Проверяем, сдали ли результаты ВСЕ игроки
    bool allPlayersReported = g.Players.All(p => p.Score > 0 || p.TimeSeconds > 0);

    if (allPlayersReported && g.Status != GameStatus.Finished)
    {
        // Помечаем как завершённую
        g.Status = GameStatus.Finished;
        Console.WriteLine($"[SERVER] Игра {req.GameId} завершена: все игроки сдали результаты.");

        // 👇 САМОЕ ВАЖНОЕ: удаляем игру из хранилища
        storage.DeleteGame(req.GameId);
        Console.WriteLine($"[SERVER] Игра {req.GameId} удалена из хранилища.");
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


// ⭐⭐⭐ 5) Получить результаты игры (для сетевой статистики)
// GET /results/{id}
/*app.MapGet("/results/{id}", (string id) =>
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

    return Results.Ok(results);
});*/


// ⭐⭐⭐ НОВЫЙ ЭНДПОИНТ: Проверка статуса игры
// GET /game/status/{id}
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



/*app.MapGet("/results/{id}", (string id) =>
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

    // ⭐ СНАЧАЛА отдаём результаты клиенту
    var response = Results.Ok(results);

    // ⭐ А ПОТОМ удаляем игру, если она завершена
    //if (game.Status == GameStatus.Finished)
    //{
    //    Console.WriteLine($"[SERVER] Игра {id} удалена после выдачи результатов.");
    //    storage.DeleteGame(id);
    //}
   // Проверяем, отправили ли ВСЕ игроки результаты
    bool allPlayersReported =
        game.Players.All(p => p.Score > 0 || p.TimeSeconds > 0);

    if (allPlayersReported)
    {
        Console.WriteLine($"[SERVER] Игра {id} удалена после выдачи результатов.");
        game.Status = GameStatus.Finished;
    }



    return response;
});*/
// ⭐⭐ ПРОСТО ОТДАЧА РЕЗУЛЬТАТОВ ⭐⭐
// GET /results/{id}
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

    return Results.Ok(results);
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
