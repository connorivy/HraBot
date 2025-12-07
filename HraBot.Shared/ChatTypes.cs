using System.Text.Json.Serialization;

namespace HraBot.Api;

public enum ChatRole
{
    System,
    Assistant,
    User
}

public class ChatMessageDto
{
    [JsonPropertyName("role")]
    public required ChatRole Role { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }
}

public class ChatRequestDto
{
    [JsonPropertyName("messages")]
    public required List<ChatMessageDto> Messages { get; set; }
}
