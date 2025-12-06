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

public static class ChatRoleMapper
{
    public static Microsoft.Extensions.AI.ChatRole Map(ChatRole role) => role switch
    {
        ChatRole.System => Microsoft.Extensions.AI.ChatRole.System,
        ChatRole.User => Microsoft.Extensions.AI.ChatRole.User,
        ChatRole.Assistant => Microsoft.Extensions.AI.ChatRole.Assistant,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}
