using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using CrosswordServer.Models;
//using System.Xml;

namespace CrosswordServer.Storage
{
    public class GameStorage
    {
        // -----------------------------
        // Модель записи рейтинга
        // -----------------------------
        public class ScoreRecord
        {
            public string PlayerName { get; set; } = "";
            public int Score { get; set; }
            public int TimeSeconds { get; set; }
            public string Difficulty { get; set; } = "";
            public DateTime Date { get; set; }

            public string TimeFormatted => TimeSpan.FromSeconds(TimeSeconds).ToString(@"mm\:ss");
        }

        public void SaveGlobalScores()
        {
            try
            {
                string json = JsonConvert.SerializeObject(GlobalScores, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(RatingFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SERVER] Ошибка сохранения рейтинга: " + ex.Message);
            }
        }


        // -----------------------------
        // Путь к файлу глобального рейтинга
        // -----------------------------
        private static string RatingFile =>
            Path.Combine(AppContext.BaseDirectory, "global_scores.json");

        // -----------------------------
        // Глобальный рейтинг
        // -----------------------------
        public List<ScoreRecord> GlobalScores { get; set; } = new();

        // -----------------------------
        // Загрузка рейтинга при старте
        // -----------------------------
        public void LoadGlobalScores()
        {
            if (!File.Exists(RatingFile))
            {
                GlobalScores = new List<ScoreRecord>();
                return;
            }

            try
            {
                string json = File.ReadAllText(RatingFile);
                GlobalScores = JsonConvert.DeserializeObject<List<ScoreRecord>>(json)
                               ?? new List<ScoreRecord>();
            }
            catch
            {
                GlobalScores = new List<ScoreRecord>();
            }
        }

        // -----------------------------
        // Сохранение рейтинга
        // -----------------------------
        //public void SaveGlobalScores()
        //{
        //    try
        //    {
        //        string json = JsonConvert.SerializeObject(GlobalScores, Formatting.Indented);
        //        File.WriteAllText(RatingFile, json);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("[SERVER] Ошибка сохранения рейтинга: " + ex.Message);
        //    }
        //}

        // -----------------------------
        // Хранилище игр
        // -----------------------------
        private readonly Dictionary<string, GameInfo> _games = new();

        // Создать новую игру
        public GameInfo CreateGame(string creatorName, string difficulty)
        {
            string id = new Random().Next(100000, 999999).ToString();
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

        // Получить список всех активных игр
        public List<GameInfo> GetAllGames() => _games.Values.ToList();

        // Получить игру по ID
        public GameInfo? GetGame(string id)
        {
            _games.TryGetValue(id, out var game);
            return game;
        }

        // Подключить игрока
        public bool JoinGame(string id, string playerName)
        {
            if (!_games.TryGetValue(id, out var game))
                return false;

            if (game.Players.Any(p => p.PlayerName == playerName))
                return true;

            game.Players.Add(new GamePlayer { PlayerName = playerName });

            if (game.Players.Count >= 1)
                game.Status = GameStatus.Running;

            return true;
        }

        // Игрок отправляет результат
        public bool SubmitResult(string id, string playerName, int score, int time)
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

            bool allFinished = game.Players.All(p => p.Score != 0 || p.TimeSeconds != 0);

            if (allFinished)
            {
                Console.WriteLine($"[SERVER] Игра {id} завершена.");
                game.Status = GameStatus.Finished;
            }

            // -----------------------------
            // Добавляем в глобальный рейтинг
            // -----------------------------
            GlobalScores.Add(new ScoreRecord
            {
                PlayerName = playerName,
                Score = score,
                TimeSeconds = time,
                Difficulty = game.Difficulty,
                Date = DateTime.UtcNow
            });

            SaveGlobalScores();

            return true;
        }

        // Удалить игру
        public void DeleteGame(string id)
        {
            _games.Remove(id);
        }

        // Получить результаты игры
        public List<GamePlayer>? GetResults(string id)
        {
            if (!_games.TryGetValue(id, out var game))
                return null;

            return game.Players
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.TimeSeconds)
                .ToList();
        }

        // Глобальный чат
        public List<ChatMessage> GlobalChat { get; set; } = new();
    }
}
