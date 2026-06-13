using System;
using System.Collections.Generic;

namespace CrosswordServer.Models
{
    /// Статус игры – нужен, чтобы в списке игр показывать:
    /// "Ожидает игроков", "Идёт", "Завершена".
    public enum GameStatus
    {
        Waiting,   // игра создана, но ещё мало игроков / не началась
        Running,   // игра идёт
        Finished   // игра завершена
    }

    /// Информация об игроке в конкретной игре.
    public class GamePlayer
    {
        /// Имя игрока (то самое, которое ребёнок вводит в игре один раз).
        public string PlayerName { get; set; } = string.Empty;
        /// Набранные очки (можно заполнять позже, когда игрок закончит игру).
        public int Score { get; set; }
        /// Время прохождения в секундах (тоже можно заполнить позже).
        public int TimeSeconds { get; set; }
    }

    /// Основная модель игры, которая будет храниться на сервере.
    public class GameInfo
    {
        /// Уникальный ID игры, который видят дети (например "123456").
        public string GameId { get; set; } = string.Empty;

        /// Имя создателя игры (Вася, Маша и т.п.).
        public string CreatorName { get; set; } = string.Empty;

        /// Сид для генерации кроссворда – чтобы у всех был одинаковый вариант.
        public int Seed { get; set; }

        /// Сложность (если нужна: "Лёгкая", "Средняя", "Сложная").
        /// Можно оставить пустой строкой, если пока не используем.
        public string Difficulty { get; set; } = string.Empty;

        /// Текущий статус игры: ожидает, идёт, завершена.
        public GameStatus Status { get; set; } = GameStatus.Waiting;

        /// Список всех игроков, которые участвуют в этой игре.
        public List<GamePlayer> Players { get; set; } = new List<GamePlayer>();

        /// Время создания игры – пригодится, если потом захотим
        /// удалять старые игры или сортировать список.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
