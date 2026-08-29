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
        //public class GamePlayer
        //{
        //    public string PlayerName { get; set; } = "";
        //    public int Score { get; set; }
        //    public int TimeSeconds { get; set; }
        //    public bool HasReported { get; set; }

        //    public string TimeFormatted => TimeSpan.FromSeconds(TimeSeconds).ToString(@"mm\:ss");
        //}


    /// Основная модель игры, которая будет храниться на сервере.
    public class GameInfo
    {
        /// Уникальный ID игры, который видят дети (например "123456").
        public string GameId { get; set; } = string.Empty;

        /// Имя создателя игры (Вася, Маша и т.п.).
        public string CreatorName { get; set; } = string.Empty;

        /// Сид для генерации кроссворда – чтобы у всех был одинаковый вариант.
        public int Seed { get; set; }
        //public List<Formula> Formulas { get; set; } = new(); // <-- Хранить готовые формулы
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
        public DateTime StartTime { get; set; }
        public int ResultsRequestsCount { get; set; } = 0;
        public List<ChatMessage> GlobalChat { get; set; } = new();





    }
}
