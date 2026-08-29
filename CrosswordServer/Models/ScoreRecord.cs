namespace CrosswordServer.Storage
{
    public class ScoreRecord
    {
        public string PlayerName { get; set; } = "";
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
        public string Difficulty { get; set; } = "";
        public DateTime Date { get; set; }

        public string TimeFormatted => TimeSpan.FromSeconds(TimeSeconds).ToString(@"mm\\:ss");
    }
}
