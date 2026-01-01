using System.Text.Json;
using HraBot.Api.Features.Json;
using HraBot.Api.Features.Workflows;
using HraBot.Core.Features.Chat;
using Microsoft.Playwright;

namespace HraBot.Tests.E2E;

public class UiTests : PageTestBase
{
    [Test]
    public async Task SendingPositiveFeedback_ShouldCreateCorrectDataInDb()
    {
        await this.Page.RouteAsync(
            "**/api/chat",
            async route =>
            {
                Console.WriteLine("hrabot api call intercepted");
                // Force latency so the typing indicator has time to render
                await Task.Delay(1000);

                // var x = await SetupTestsE2E.ApiClient.Api.Chat.PostAsync(new());
                ApprovedResponseContract approvedResponse = new(
                    1,
                    1,
                    ResponseType.Success,
                    "This is a dummy response",
                    [new("filename", "this is a dummy citation")]
                );

                var bodyJson = JsonSerializer.Serialize(
                    approvedResponse,
                    HraBotJsonSerializerContext.Default.ApprovedResponseContract
                );

                await route.FulfillAsync(
                    new RouteFulfillOptions
                    {
                        Status = 200,
                        ContentType = "application/json",
                        Body = bodyJson,
                    }
                );
            }
        );

        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        var sendButton = this.Page.GetByRole(AriaRole.Button);
        await Expect(sendButton).ToHaveCountAsync(1);
        await Expect(sendButton).ToBeVisibleAsync();
        // send button should be disabled until the user adds input
        await Expect(sendButton).ToBeDisabledAsync();

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        await Expect(chatBox).ToHaveCountAsync(1);
        await Expect(chatBox).ToBeVisibleAsync();
        await chatBox.FillAsync("hello!");
        await Expect(sendButton).Not.ToBeDisabledAsync();

        var typingBubbles = this.Page.GetByTestId("assistant-typing");
        await Expect(typingBubbles).ToBeHiddenAsync();

        await sendButton.ClickAsync();
        await Expect(typingBubbles).ToBeVisibleAsync();

        await Expect(typingBubbles).ToBeHiddenAsync(new() { Timeout = 3000 });
    }
}
