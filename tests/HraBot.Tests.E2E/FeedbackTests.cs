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
            resp => resp.Url.Contains("/api/chat") && resp.Request.Method == "POST"
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
        // there should be three buttons. The last one is the send button, currently disabled, and two new feedback buttons (thumbs up and thumbs down)
        await Expect(buttons).ToHaveCountAsync(3);

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var thumbsUp = buttons.Nth(0);
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

        Console.WriteLine($"Persisted Feedback: {JsonSerializer.Serialize(persistedFeedback)}");
        persistedFeedback.MessageFeedbackItemIds.Should().HaveCount(3);
        persistedFeedback.MessageFeedbackItemIds.Should().Contain([1, 2, 3]);

        await Expect(chatBox).Not.ToBeDisabledAsync();
    }

    private async Task SendNegativeFeedback()
    {
        var buttons = this.Page.GetByRole(AriaRole.Button);
        // there should be three buttons. The last one is the send button, currently disabled, and two new feedback buttons (thumbs up and thumbs down)
        await Expect(buttons).ToHaveCountAsync(3);

        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var thumbsDown = buttons.Nth(1);

        await thumbsDown.ClickAsync();

        var dialog = this.Page.GetByRole(AriaRole.Dialog);

        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToContainTextAsync("This is a dummy response");
        await Expect(dialog).ToContainTextAsync("dummy-quote");

        var contentSelect = dialog.GetByRole(AriaRole.Combobox, new() { Name = "Content" });
        var citationsSelect = dialog.GetByRole(AriaRole.Combobox, new() { Name = "Citations" });
        var importanceSelect = dialog.GetByRole(AriaRole.Combobox, new() { Name = "Importance" });
        var otherComments = dialog.GetByRole(AriaRole.Textbox, new() { Name = "Other comments" });

        await Expect(contentSelect).ToHaveValueAsync("no issues");
        await Expect(citationsSelect).ToHaveValueAsync("no issues");
        await Expect(importanceSelect).ToHaveValueAsync("");

        var submitButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Submit" });
        await Expect(submitButton).ToBeDisabledAsync();

        await contentSelect.SelectOptionAsync(new SelectOptionValue { Label = "incorrect" });
        await Expect(submitButton).ToBeDisabledAsync();

        await citationsSelect.SelectOptionAsync(new SelectOptionValue { Label = "missing" });
        await Expect(submitButton).ToBeDisabledAsync();

        await importanceSelect.SelectOptionAsync(new SelectOptionValue { Value = "4" });
        await otherComments.FillAsync("Needs clearer citation details.");
        await Expect(submitButton).Not.ToBeDisabledAsync();

        var response = await this.Page.RunAndWaitForResponseAsync(
            async () => await submitButton.ClickAsync(),
            resp => resp.Url.Contains("/api/feedback") && resp.Request.Method == "POST" && resp.Ok
        );
        var json =
            await response.JsonAsync()
            ?? throw new InvalidOperationException("Json response is null");
        Console.WriteLine($"Received api response: {json}");
        var typedResponse =
            json.Deserialize(HraBotJsonSerializerContext.Default.EntityResponseInt64)
            ?? throw new InvalidOperationException($"Could not deserialize json: {json}");

        var persistedFeedback =
            await SetupTestsE2E.ApiClient.Api.Feedback[typedResponse.Id].GetAsync()
            ?? throw new InvalidOperationException($"Could not find persisted feedback");

        Console.WriteLine($"Persisted Feedback: {JsonSerializer.Serialize(persistedFeedback)}");
        persistedFeedback.MessageFeedbackItemIds.Should().HaveCount(2);
        persistedFeedback.MessageFeedbackItemIds.Should().Contain([4, 9]);
        persistedFeedback.ImportanceToTakeCommand.Should().Be(4);
        persistedFeedback.AdditionalComments.Should().Be("Needs clearer citation details.");
    }
}
