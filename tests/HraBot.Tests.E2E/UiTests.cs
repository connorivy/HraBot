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
            "**/chat",
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

        var sendButton = this.Page.GetByRole(AriaRole.Button, new() { Name = "Send message" });
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

    [Test]
    public async Task AfterFiveMessages_ShouldShowLimitNoticeAndDisableInput()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        var sendButton = this.Page.GetByRole(AriaRole.Button, new() { Name = "Send message" });
        var chatBox = this.Page.GetByRole(AriaRole.Textbox);

        for (var i = 0; i < 5; i++)
        {
            await FeedbackTests.SendMessage(this.Page, $"hello {i + 1}");

            // Provide feedback to re-enable the input for the next message (except the final send)
            if (i < 4)
            {
                await FeedbackTests.SendFeedback(this.Page, 5, 5);
                await Expect(chatBox).Not.ToBeDisabledAsync(new() { Timeout = 2000 });
            }
        }

        var limitNotice = this.Page.GetByText(
            "You have reached the message limit for this chat. Please start a new conversation."
        );

        await Expect(limitNotice).ToBeVisibleAsync();
        await Expect(chatBox).ToBeDisabledAsync();
        await Expect(sendButton).ToBeDisabledAsync();

        var newChatButton = this.Page.GetByRole(AriaRole.Button, new() { Name = "New chat" });
        await Expect(newChatButton).Not.ToBeDisabledAsync();
    }
}
