namespace OfisYonetimSistemi.Models.ViewModels
{
    public class ChatCommandResponse
    {
        public string Action { get; set; } = string.Empty;

        public string ResponseText { get; set; } = string.Empty;

        public bool IsSuccessful { get; set; }

        public List<string> Suggestions { get; set; } = new();
    }
}
