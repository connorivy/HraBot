using System.Diagnostics;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace HraBot.Core.Features.Chat;

[HraBotEndpoint(Http.Post, "/chat")]
public partial class Chat(
    HraBotDbContext hraBotDbContext,
    GetApprovedResponseWorkflow returnApprovedResponse
) : BaseEndpoint<ChatRequest, ApprovedResponseContract>
{
    public override async Task<Result<ApprovedResponseContract>> ExecuteRequestAsync(
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

        conversation.AddTrackedMessage(Role.User, req.Content);

        var approvedResponse = await returnApprovedResponse.GetApprovedResponse(
            conversation.Messages.Select(m => new ChatMessage(
                ChatRoleMapper.Map(m.Role),
                m.Content
            )),
            ct
        );
        var newMessage = conversation.AddTrackedMessage(Role.Ai, approvedResponse.Response);
        await hraBotDbContext.SaveChangesAsync(ct);
        return new ApprovedResponseContract(
            conversation.Id,
            newMessage.Id,
            approvedResponse.ResponseType,
            approvedResponse.Response,
            approvedResponse.Citations
        );
    }

    private async ValueTask<Result<Conversation>> GetOrCreateConversation(long? conversationId)
    {
        if (conversationId is null)
        {
            var newConversation = new Conversation() { Messages = [] };
            hraBotDbContext.Add(newConversation);
            return newConversation;
        }

        // need to make local declarations for variables
        // https://github.com/dotnet/efcore/issues/35887
        var localId = conversationId;
        var localContext = hraBotDbContext;
        var existingConversation = await localContext
            .Conversations.Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == localId);
        if (existingConversation is null)
        {
            return HraBotError.NotFound(
                description: $"Could not find conversation with Id = {conversationId}"
            );
        }
        return existingConversation;
    }

    // public override void Configure(IEndpointRouteBuilder builder)
    // {
    //     builder.MapPost(
    //         "/chat",
    //         (
    //             [Microsoft.AspNetCore.Mvc.FromServices] Chat endpoint,
    //             [Microsoft.AspNetCore.Mvc.FromBody] ChatRequest request
    //         ) => endpoint.ExecuteAsync(request)
    //     );
    // }
}

public record ChatRequest(long? ConversationId, string Content);

public record ApprovedResponseContract(
    long ConversationId,
    long MessageId,
    ResponseType ResponseType,
    string Response,
    List<Citation> Citations
);

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
