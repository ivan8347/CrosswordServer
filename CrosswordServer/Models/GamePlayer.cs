namespace CrosswordServer.Models
{
    public class GamePlayer
    {
        public string PlayerName { get; set; } = "";
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
        public bool HasReported { get; set; }

        public string TimeFormatted => TimeSpan.FromSeconds(TimeSeconds).ToString(@"mm\\:ss");
    }
}
