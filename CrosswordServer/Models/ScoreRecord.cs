namespace CrosswordServer.Models
{
    public class ScoreRecord
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
        public string Difficulty { get; set; }
        public DateTime Date { get; set; }
    }

}
