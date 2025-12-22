using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

sealed class StartExecutor() : Executor("ConcurrentStartExecutor")
{
    protected override Microsoft.Agents.AI.Workflows.RouteBuilder ConfigureRoutes(
        Microsoft.Agents.AI.Workflows.RouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .AddHandler<List<ChatMessage>>(this.RouteMessages)
            .AddHandler<TurnToken>(this.RouteTurnTokenAsync);
    }

    private ValueTask RouteMessages(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken
    )
    {
        return context.SendMessageAsync(messages, cancellationToken: cancellationToken);
    }

    private ValueTask RouteTurnTokenAsync(
        TurnToken token,
        IWorkflowContext context,
        CancellationToken cancellationToken
    )
    {
        return context.SendMessageAsync(token, cancellationToken: cancellationToken);
    }
}
