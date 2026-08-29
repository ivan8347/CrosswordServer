using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using CrosswordServer.Models;

namespace CrosswordServer.Storage
{
    public class GameStorage
    {
        private static string RatingFile =>
            Path.Combine(AppContext.BaseDirectory, "global_scores.json");

        public List<ScoreRecord> GlobalScores { get; set; } = new();

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

        private readonly Dictionary<string, GameInfo> _games = new();

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

        public List<GameInfo> GetAllGames() => _games.Values.ToList();

        public GameInfo? GetGame(string id)
        {
            _games.TryGetValue(id, out var game);
            return game;
        }

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

            GlobalScores.Add(new ScoreRecord
            {
                PlayerName = playerName,
                Score = score,
                TimeSeconds = time,
                Difficulty = game.Difficulty,
                Date = DateTime.UtcNow
            });

            SaveGlobalScores();

            bool allFinished = game.Players.All(p => p.HasReported);

            if (allFinished)
            {
                game.Status = GameStatus.Finished;
                _games.Remove(id);
            }

            return true;
        }

        public void DeleteGame(string id)
        {
            _games.Remove(id);
        }

        public List<GamePlayer>? GetResults(string id)
        {
            if (!_games.TryGetValue(id, out var game))
                return null;

            return game.Players
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.TimeSeconds)
                .ToList();
        }

        public List<ChatMessage> GlobalChat { get; set; } = new();
    }
}
