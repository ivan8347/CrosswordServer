namespace CrosswordServer.Models
{
    public class ChatMessage
    {
       
            public string Player { get; set; } = "";
            public string Text { get; set; } = "";
            public DateTime Time { get; set; }
    }
}
