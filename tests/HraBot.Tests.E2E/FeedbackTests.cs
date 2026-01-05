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
        await SendFeedback(5, 1);
    }

    [Test]
    public async Task SendingOneStarFeedback_ShouldCreateCorrectDataInDb()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        await SendMessage("hello!");
        await SendFeedback(1, 5);
    }

    private async Task SendMessage(string message) => await SendMessage(this.Page, message);

    public static async Task SendMessage(IPage page, string message)
    {
        var sendButton = page.GetByRole(AriaRole.Button, new() { Name = "Send message" });
        await Assertions.Expect(sendButton).ToHaveCountAsync(1);
        await Assertions.Expect(sendButton).ToBeVisibleAsync();
        // send button should be disabled until the user adds input
        await Assertions.Expect(sendButton).ToBeDisabledAsync();

        var chatBox = page.GetByRole(AriaRole.Textbox);
        await Assertions.Expect(chatBox).ToHaveCountAsync(1);
        await Assertions.Expect(chatBox).ToBeVisibleAsync();
        await chatBox.FillAsync(message);
        await Assertions.Expect(sendButton).Not.ToBeDisabledAsync();

        var response = await page.RunAndWaitForResponseAsync(
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

    private async Task SendFeedback(byte rating, byte importance) =>
        await SendFeedback(this.Page, rating, importance);

    public static async Task SendFeedback(IPage page, byte rating, byte importance)
    {
        var chatBox = page.GetByRole(AriaRole.Textbox);
        // chat box should be disabled until feed back is provided. There should be a tooltip that explains this
        await Assertions.Expect(chatBox).ToBeDisabledAsync();

        var ratingGroup = page.GetByRole(AriaRole.Group, new() { Name = "Response rating" });
        await Assertions.Expect(ratingGroup).ToBeVisibleAsync();

        string ratingButton = rating switch
        {
            1 => "Very poor",
            2 => "Poor",
            3 => "Okay",
            4 => "Good",
            5 => "Excellent",
            _ => throw new NotImplementedException(),
        };

        var starButton = ratingGroup.GetByRole(
            AriaRole.Button,
            new() { Name = ratingButton, Exact = true }
        );
        var importanceGroup = page.GetByRole(
            AriaRole.Group,
            new() { Name = "Response importance" }
        );
        var importanceButton = importanceGroup.GetByRole(
            AriaRole.Button,
            new() { Name = $"Importance {importance}" }
        );

        var response = await page.RunAndWaitForResponseAsync(
            async () =>
            {
                await starButton.ClickAsync();
                await importanceButton.ClickAsync();
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
        persistedFeedback.Rating.Should().Be(rating);
        persistedFeedback.ImportanceToTakeCommand.Should().Be(importance);
    }
}
