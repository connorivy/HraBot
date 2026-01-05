using System.Text.Json;
using AwesomeAssertions;
using HraBot.Api.Features.Json;
using Microsoft.Playwright;

namespace HraBot.Tests.E2E;

public class FeedbackTests : PageTestBase
{
    [Test]
    public async Task SendingFiveStarFeedback_ShouldCreateCorrectDataInDb()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        await this.Expect(this.PageContext.Page)
            .ToHaveTitleAsync("JackBot", new() { Timeout = 500 });

        await SendMessage("hello!");
        await SendFiveStarFeedback();
    }

    [Test]
    public async Task SendingTwoStarFeedback_ShouldCreateCorrectDataInDb()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        await SendMessage("hello!");
        await SendTwoStarFeedback();
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

    private async Task SendFiveStarFeedback()
    {
        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var ratingGroup = this.Page.GetByRole(AriaRole.Group, new() { Name = "Response rating" });
        await Expect(ratingGroup).ToBeVisibleAsync();

        var fiveStarButton = ratingGroup.GetByRole(AriaRole.Button, new() { Name = "Excellent" });
        var importanceGroup = this.Page.GetByRole(
            AriaRole.Group,
            new() { Name = "Response importance" }
        );
        var importanceFive = importanceGroup.GetByRole(
            AriaRole.Button,
            new() { Name = "Importance 5" }
        );

        var response = await this.Page.RunAndWaitForResponseAsync(
            async () =>
            {
                await fiveStarButton.ClickAsync();
                await importanceFive.ClickAsync();
            },
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
        persistedFeedback.Rating.Should().Be(5);
        persistedFeedback.ImportanceToTakeCommand.Should().Be(5);

        await Expect(chatBox).Not.ToBeDisabledAsync();
    }

    private async Task SendTwoStarFeedback()
    {
        var chatBox = this.Page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Expect(chatBox).ToBeDisabledAsync();

        var ratingGroup = this.Page.GetByRole(AriaRole.Group, new() { Name = "Response rating" });
        await Expect(ratingGroup).ToBeVisibleAsync();

        var twoStarButton = ratingGroup.GetByRole(
            AriaRole.Button,
            new() { Name = "Poor", Exact = true }
        );
        var importanceGroup = this.Page.GetByRole(
            AriaRole.Group,
            new() { Name = "Response importance" }
        );
        var importanceTwo = importanceGroup.GetByRole(
            AriaRole.Button,
            new() { Name = "Importance 2" }
        );

        var response = await this.Page.RunAndWaitForResponseAsync(
            async () =>
            {
                await twoStarButton.ClickAsync();
                await importanceTwo.ClickAsync();
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
        persistedFeedback.Rating.Should().Be(2);
        persistedFeedback.ImportanceToTakeCommand.Should().Be(2);
    }
}
