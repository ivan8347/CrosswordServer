using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using CrosswordServer.Models;

namespace CrosswordServer.Storage
{
    public class GameStorage
    {
        // -----------------------------
        // Глобальный рейтинг
        // -----------------------------
        public class ScoreRecord
        {
            public string PlayerName { get; set; } = "";
            public int Score { get; set; }
            public int TimeSeconds { get; set; }
            public string Difficulty { get; set; } = "";
            public DateTime Date { get; set; }

            public string TimeFormatted =>
                $"{TimeSeconds / 60:D2}:{TimeSeconds % 60:D2}";
        }

        private readonly Dictionary<string, GameInfo> _games = new();
        private readonly object _lock = new();

        public List<ScoreRecord> GlobalScores { get; set; } = new();

        // -----------------------------
        // Создание игры — сложность задаёт ТОЛЬКО создатель
        // -----------------------------
        public GameInfo CreateGame(string creatorName, string difficulty)
        {
            lock (_lock)
            {
                string id = new Random().Next(100000, 999999).ToString();
                int seed = Random.Shared.Next(1, 999999);

                var game = new GameInfo
                {
                    GameId = id,
                    CreatorName = creatorName,
                    Difficulty = difficulty,   // ← ВАЖНО: сложность задаётся здесь
                    Seed = seed,
                    Status = GameStatus.Waiting,
                    StartTime = DateTime.UtcNow
                };

                game.Players.Add(new GamePlayer { PlayerName = creatorName });

                _games[id] = game;
                return game;
            }
        }

        // -----------------------------
        // Получение списка игр
        // -----------------------------
        public List<GameInfo> GetAllGames()
        {
            lock (_lock)
            {
                return _games.Values.ToList();
            }
        }

        // -----------------------------
        // Получение игры по ID
        // -----------------------------
        public GameInfo? GetGame(string id)
        {
            lock (_lock)
            {
                _games.TryGetValue(id, out var game);
                return game;
            }
        }

        // -----------------------------
        // Подключение игрока
        // Сложность НЕ меняется!
        // -----------------------------
        public bool JoinGame(string id, string playerName)
        {
            lock (_lock)
            {
                if (!_games.TryGetValue(id, out var game))
                    return false;

                if (game.Players.Any(p => p.PlayerName == playerName))
                    return true;

                game.Players.Add(new GamePlayer { PlayerName = playerName });

                if (game.Players.Count >= 2)
                    game.Status = GameStatus.Running;

                return true;
            }
        }

        // -----------------------------
        // Отправка результата
        // -----------------------------
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

                bool allFinished = game.Players.All(p => p.HasReported);

                if (allFinished)
                {
                    Console.WriteLine($"[SERVER] Игра {id} завершена.");
                    game.Status = GameStatus.Finished;
                }

                return true;
            }
        }

        // -----------------------------
        // Удаление игры
        // -----------------------------
        public void DeleteGame(string id)
        {
            lock (_lock)
            {
                _games.Remove(id);
            }
        }

        // -----------------------------
        // Получение результатов игры
        // -----------------------------
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

        // -----------------------------
        // Глобальный чат
        // -----------------------------
        public List<ChatMessage> GlobalChat { get; set; } = new();
    }
}
