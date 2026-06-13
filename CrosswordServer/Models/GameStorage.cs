using System;
using System.Collections.Generic;
using System.Linq;
using CrosswordServer.Models;

namespace CrosswordServer.Storage
{
    // Хранилище всех игр на сервере.
    // Здесь создаём игры, подключаем игроков, принимаем результаты и удаляем завершённые игры.
    public class GameStorage
    {
        // Все активные игры.
        // Ключ — GameId (например "123456").
        // Значение — GameInfo.
        private readonly Dictionary<string, GameInfo> _games = new();

        // Создать новую игру.
        public GameInfo CreateGame(string creatorName, string difficulty)
        {
            // Генерируем короткий ID игры (6 цифр).
            string id = new Random().Next(100000, 999999).ToString();

            // Генерируем seed для одинакового кроссворда.
            int seed = Random.Shared.Next(1, 999999);

            var game = new GameInfo
            {
                GameId = id,
                CreatorName = creatorName,
                Difficulty = difficulty,
                Seed = seed,
                Status = GameStatus.Waiting
            };

            // Создатель автоматически становится первым игроком.
            game.Players.Add(new GamePlayer
            {
                PlayerName = creatorName
            });

            _games[id] = game;
            return game;
        }

        // Получить список всех активных игр.
        // Завершённые игры сюда не попадают, потому что мы их удаляем.
        public List<GameInfo> GetAllGames()
        {
            return _games.Values.ToList();
        }

        // Получить игру по ID.
        public GameInfo? GetGame(string id)
        {
            _games.TryGetValue(id, out var game);
            return game;
        }

        // Подключить игрока к игре.
        public bool JoinGame(string id, string playerName)
        {
            if (!_games.TryGetValue(id, out var game))
                return false;

            // Если игрок уже есть — ничего не делаем.
            if (game.Players.Any(p => p.PlayerName == playerName))
                return true;

            game.Players.Add(new GamePlayer
            {
                PlayerName = playerName
            });

            // Если игроков стало больше одного — считаем, что игра идёт.
            if (game.Players.Count > 1)
                game.Status = GameStatus.Running;

            return true;
        }

        // Игрок отправляет результат.
        // После того как все игроки отправили результат — игра удаляется.
        public bool SubmitResult(string id, string playerName, int score, int time)
        {
            if (!_games.TryGetValue(id, out var game))
                return false;

            var player = game.Players.FirstOrDefault(p => p.PlayerName == playerName);
            if (player == null)
                return false;

            player.Score = score;
            player.TimeSeconds = time;

            // Проверяем: все ли игроки завершили игру?
            bool allFinished = game.Players.All(p => p.Score > 0 || p.TimeSeconds > 0);

            if (allFinished)
            {
                // Помечаем игру завершённой.
                game.Status = GameStatus.Finished;

                // Удаляем игру из списка.
                _games.Remove(id);
            }

            return true;
        }
    }
}
