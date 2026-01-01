using System.Text.Json;
using AwesomeAssertions;
using HraBot.Api.Features.Json;
using Microsoft.Playwright;

namespace HraBot.Tests.E2E;

public class ResponseTests : PageTestBase
{
    [Test]
    public async Task WhenUserAsksQuestion_AgentResponseShouldShow()
    {
        // await this.Page.RouteAsync(
        //     "**/api/chat",
        //     async route =>
        //     {
        //         Console.WriteLine("hrabot api call intercepted");
        //         // Force latency so the typing indicator has time to render
        //         await Task.Delay(1000);

        //         var x = await SetupTestsE2E.ApiClient.Api.Chat.PostAsync(new());
        //         ApprovedResponseContract approvedResponse = new(
        //             ResponseType.Success,
        //             "This is a dummy response",
        //             [new("filename", "this is a dummy citation")]
        //         );

        //         var bodyJson = JsonSerializer.Serialize(
        //             approvedResponse,
        //             HraBotJsonSerializerContext.Default.ApprovedResponse
        //         );

        //         await route.FulfillAsync(
        //             new RouteFulfillOptions
        //             {
        //                 Status = 200,
        //                 ContentType = "application/json",
        //                 Body = bodyJson,
        //             }
        //         );
        //     }
        // );

        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        await this.Expect(this.PageContext.Page)
            .ToHaveTitleAsync("JackBot", new() { Timeout = 500 });

        await SendMessage("hello!");
        await SendPositiveFeedback();
    }

    private async Task SendMessage(string message)
    {
        var sendButton = this.Page.GetByRole(AriaRole.Button);
        await Expect(sendButton).ToHaveCountAsync(1);
        await Expect(sendButton).ToBeVisibleAsync();
        // send button should be disabled until the user adds input
        await Expect(sendButton).ToBeDisabledAsync();

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        await Expect(chatBox).ToHaveCountAsync(1);
        await Expect(chatBox).ToBeVisibleAsync();
        await chatBox.FillAsync(message);
        await Expect(sendButton).Not.ToBeDisabledAsync();

        var typingBubbles = this.Page.GetByTestId("assistant-typing");
        await Expect(typingBubbles).ToBeHiddenAsync();

        await sendButton.ClickAsync();
        await Expect(typingBubbles).ToBeVisibleAsync();

        await Expect(typingBubbles).ToBeHiddenAsync(new() { Timeout = 1000 });
    }

    private async Task SendPositiveFeedback()
    {
        var buttons = this.Page.GetByRole(AriaRole.Button);
        // there should be three buttons. The last one is the send button, currently disabled, and two new feedback buttons (thumbs up and thumbs down)
        await Expect(buttons).ToHaveCountAsync(3);

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var thumbsUp = buttons.Nth(1);
        var response = await this.Page.RunAndWaitForResponseAsync(
            async () => await thumbsUp.ClickAsync(),
            resp => resp.Ok
        );
        var json =
            await response.JsonAsync()
            ?? throw new InvalidOperationException("Json response is null");
        // var x = await SetupTestsE2E.ApiClient.Api.Feedback.PostAsync(null!);
        var typedResponse =
            json.Deserialize(HraBotJsonSerializerContext.Default.EntityResponseInt64)
            ?? throw new InvalidOperationException($"Could not deserialize json: {json}");

        var persistedFeedback =
            await SetupTestsE2E.ApiClient.Api.Feedback[typedResponse.Id].GetAsync()
            ?? throw new InvalidOperationException($"Could not find persisted feedback");

        persistedFeedback.MessageFeedbackItemIds.Should().HaveCount(1);
        persistedFeedback.MessageFeedbackItemIds.Should().Contain([1]);
    }
}
