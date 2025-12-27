using System.Diagnostics;
using HraBot.Api.Features.Workflows;
using HraBot.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace HraBot.Core.Features.Chat;

public class Chat(HraBotDbContext hraBotDbContext, ReturnApprovedResponse returnApprovedResponse)
    : BaseEndpoint<ChatRequest, ApprovedResponse>
{
    public override async Task<Result<ApprovedResponse>> ExecuteRequestAsync(
        ChatRequest req,
        CancellationToken ct = default
    )
    {
        var conversationResult = await GetOrCreateConversation(req.ConversationId);
        if (conversationResult.IsError)
        {
            return conversationResult.Error;
        }
        var conversation = conversationResult.Value;
        if (conversation.Messages is null)
        {
            throw new UnreachableException("This can't happen after the proper query");
        }

        conversation.AddMessage(Role.User, req.Content);

        var approvedResponse = await returnApprovedResponse.GetApprovedResponse(
            conversation.Messages.Select(m => new ChatMessage(
                ChatRoleMapper.Map(m.Role),
                m.Content
            )),
            ct
        );
        conversation.AddMessage(Role.Ai, approvedResponse.Response);
        await hraBotDbContext.SaveChangesAsync(ct);
        return approvedResponse;
    }

    private async ValueTask<Result<Conversation>> GetOrCreateConversation(long? conversationId)
    {
        if (conversationId is null)
        {
            var newConversation = new Conversation();
            hraBotDbContext.Add(newConversation);
            return newConversation;
        }

        var existingConversation = await hraBotDbContext
            .Conversations.Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
        if (existingConversation is null)
        {
            return HraBotError.NotFound(
                description: $"Could not find conversation with Id = {conversationId}"
            );
        }
        return existingConversation;
    }
}

public record ChatRequest(long? ConversationId, string Content);

public static class ChatRoleMapper
{
    public static Microsoft.Extensions.AI.ChatRole Map(Role role) =>
        role switch
        {
            Role.System => Microsoft.Extensions.AI.ChatRole.System,
            Role.User => Microsoft.Extensions.AI.ChatRole.User,
            Role.Ai => Microsoft.Extensions.AI.ChatRole.Assistant,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
}
