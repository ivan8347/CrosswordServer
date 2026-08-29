using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using CrosswordServer.Models;

namespace CrosswordServer.Storage
{
    public class GameStorage
    {
        public class ScoreRecord
        {
            public string PlayerName { get; set; } = "";
            public int Score { get; set; }
            public int TimeSeconds { get; set; }
            public string Difficulty { get; set; } = "";
            public DateTime Date { get; set; }

            // Форматирование времени прямо в модели
            public string TimeFormatted =>
                $"{TimeSeconds / 60:D2}:{TimeSeconds % 60:D2}";
        }

        private readonly Dictionary<string, GameInfo> _games = new();
        private readonly object _lock = new(); // Для потокобезопасности
        private static readonly Random _idGenerator = new Random(); // Для уникальных ID

        // Глобальный рейтинг (в памяти)
        public List<ScoreRecord> GlobalScores { get; set; } = new();

        // Методы для работы с файлом (оставлены, но не используются в текущей логике)
        private static string RatingFile =>
            System.IO.Path.Combine(AppContext.BaseDirectory, "global_scores.json");

        public void LoadGlobalScores()
        {
            // На Render это не будет работать корректно, поэтому лучше не вызывать
            if (!System.IO.File.Exists(RatingFile))
            {
                GlobalScores = new List<ScoreRecord>();
                return;
            }

            try
            {
                string json = System.IO.File.ReadAllText(RatingFile);
                GlobalScores = JsonConvert.DeserializeObject<List<ScoreRecord>>(json)
                               ?? new List<ScoreRecord>();
            }
            catch
            {
                GlobalScores = new List<ScoreRecord>();
            }
        }

        public void SaveGlobalScores()
        {
            // На Render запись в файл не сработает, поэтому этот метод лучше не вызывать
            try
            {
                string json = JsonConvert.SerializeObject(GlobalScores, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(RatingFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SERVER] Ошибка сохранения рейтинга: " + ex.Message);
            }
        }

        public GameInfo CreateGame(string creatorName, string difficulty)
        {
            lock (_lock)
            {
                string id = _idGenerator.Next(100000, 999999).ToString();
                int seed = Random.Shared.Next(1, 999999);

                var game = new GameInfo
                {
                    GameId = id,
                    CreatorName = creatorName,
                    Difficulty = difficulty,
                    Seed = seed,
                    Status = GameStatus.Waiting,
                    StartTime = DateTime.UtcNow
                };

                game.Players.Add(new GamePlayer { PlayerName = creatorName });

                _games[id] = game;
                return game;
            }
        }

        public List<GameInfo> GetAllGames()
        {
            lock (_lock)
            {
                return _games.Values.ToList();
            }
        }

        public GameInfo? GetGame(string id)
        {
            lock (_lock)
            {
                _games.TryGetValue(id, out var game);
                return game;
            }
        }

        public bool JoinGame(string id, string playerName)
        {
            lock (_lock)
            {
                if (!_games.TryGetValue(id, out var game))
                    return false;

                if (game.Players.Any(p => p.PlayerName == playerName))
                    return true;

                game.Players.Add(new GamePlayer { PlayerName = playerName });

                // ✅ Игра стартует, когда есть минимум 2 игрока
                if (game.Players.Count >= 2)
                    game.Status = GameStatus.Running;

                return true;
            }
        }

        public bool SubmitResult(string id, string playerName, int score, int time)
        {
            lock (_lock)
            {
                if (!_games.TryGetValue(id, out var game))
                    return false;

                var player = game.Players.FirstOrDefault(p => p.PlayerName == playerName);
                if (player == null)
                    return false;

                player.Score = score;
                player.TimeSeconds = time;
                player.HasReported = true;

                Console.WriteLine($"[SERVER] Игрок {playerName} отправил результат: Score={score}, Time={time}");

                // ✅ Проверка по флагу HasReported
                bool allFinished = game.Players.All(p => p.HasReported);

                if (allFinished)
                {
                    Console.WriteLine($"[SERVER] Игра {id} завершена.");
                    game.Status = GameStatus.Finished;
                    // Удаление игры лучше делать в Program.cs
                }

                // ❌ УДАЛЕНО: добавление в GlobalScores и SaveGlobalScores
                // Они будут добавлены в Program.cs в эндпоинте /game/result

                return true;
            }
        }

        public void DeleteGame(string id)
        {
            lock (_lock)
            {
                _games.Remove(id);
            }
        }

        public List<GamePlayer>? GetResults(string id)
        {
            lock (_lock)
            {
                if (!_games.TryGetValue(id, out var game))
                    return null;

                return game.Players
                    .OrderByDescending(p => p.Score)
                    .ThenBy(p => p.TimeSeconds)
                    .ToList();
            }
        }

        public List<ChatMessage> GlobalChat { get; set; } = new();
    }
}
