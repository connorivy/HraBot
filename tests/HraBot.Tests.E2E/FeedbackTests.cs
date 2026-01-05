using System.Text.Json;
using AwesomeAssertions;
using HraBot.Api.Features.Json;
using Microsoft.Playwright;

namespace HraBot.Tests.E2E;

public class FeedbackTests : PageTestBase
{
    [Test]
    public async Task SendingPositiveFeedback_ShouldCreateCorrectDataInDb()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        await this.Expect(this.PageContext.Page)
            .ToHaveTitleAsync("JackBot", new() { Timeout = 500 });

        await SendMessage("hello!");
        await SendPositiveFeedback();
    }

    [Test]
    public async Task SendingNegativeFeedback_ShouldCreateCorrectDataInDb()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        await SendMessage("hello!");
        await SendNegativeFeedback();
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

        var response = await this.Page.RunAndWaitForResponseAsync(
            async () => await sendButton.ClickAsync(),
            resp => resp.Url.Contains("/chat") && resp.Request.Method == "POST"
        );
        var jsonResponse = await response.JsonAsync();
        Console.WriteLine($"Message send response: {jsonResponse}");
        if (response.Status is < 200 or >= 300)
        {
            throw new InvalidOperationException(
                $"Message send response returned unexpected status code: {response.Status}"
            );
        }
    }

    private async Task SendPositiveFeedback()
    {
        var buttons = this.Page.GetByRole(AriaRole.Button);
        // there should be three buttons. Two thumbs and the send button (disabled)
        await Expect(buttons).ToHaveCountAsync(3);

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var thumbsUp = this.Page.GetByRole(AriaRole.Button, new() { Name = "Thumbs up" });
        var importanceSelect = this.Page.GetByRole(
            AriaRole.Combobox,
            new() { Name = "Importance" }
        );

        await importanceSelect.SelectOptionAsync(new SelectOptionValue { Value = "5" });

        var response = await this.Page.RunAndWaitForResponseAsync(
            async () => await thumbsUp.ClickAsync(),
            resp => resp.Url.Contains("/feedback") && resp.Request.Method == "PUT" && resp.Ok
        );
        var json =
            await response.JsonAsync()
            ?? throw new InvalidOperationException("Json response is null");
        // var x = await SetupTestsE2E.ApiClient.Api.Feedback.PostAsync(null!);
        var typedResponse =
            json.Deserialize(HraBotJsonSerializerContext.Default.EntityResponseInt64)
            ?? throw new InvalidOperationException($"Could not deserialize json: {json}");

        var persistedFeedback =
            await SetupTestsE2E.ApiClient.Feedback[typedResponse.Id].GetAsync()
            ?? throw new InvalidOperationException($"Could not find persisted feedback");

        Console.WriteLine($"Persisted Feedback: {JsonSerializer.Serialize(persistedFeedback)}");
        persistedFeedback.IsPositive.Should().BeTrue();
        persistedFeedback.ImportanceToTakeCommand.Should().Be(5);

        await Expect(chatBox).Not.ToBeDisabledAsync();
    }

    private async Task SendNegativeFeedback()
    {
        var buttons = this.Page.GetByRole(AriaRole.Button);
        // there should be three buttons. Two thumbs and the send button (disabled)
        await Expect(buttons).ToHaveCountAsync(3);

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var thumbsDown = this.Page.GetByRole(AriaRole.Button, new() { Name = "Thumbs down" });
        var importanceSelect = this.Page.GetByRole(
            AriaRole.Combobox,
            new() { Name = "Importance" }
        );

        var response = await this.Page.RunAndWaitForResponseAsync(
            async () =>
            {
                await thumbsDown.ClickAsync();
                await importanceSelect.SelectOptionAsync(new SelectOptionValue { Value = "2" });
            },
            resp => resp.Url.Contains("/feedback") && resp.Request.Method == "PUT" && resp.Ok
        );
        var json =
            await response.JsonAsync()
            ?? throw new InvalidOperationException("Json response is null");
        Console.WriteLine($"Received api response: {json}");
        var typedResponse =
            json.Deserialize(HraBotJsonSerializerContext.Default.EntityResponseInt64)
            ?? throw new InvalidOperationException($"Could not deserialize json: {json}");

        var persistedFeedback =
            await SetupTestsE2E.ApiClient.Feedback[typedResponse.Id].GetAsync()
            ?? throw new InvalidOperationException($"Could not find persisted feedback");

        Console.WriteLine($"Persisted Feedback: {JsonSerializer.Serialize(persistedFeedback)}");
        persistedFeedback.IsPositive.Should().BeFalse();
        persistedFeedback.ImportanceToTakeCommand.Should().Be(2);
    }
}
