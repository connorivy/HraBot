using Microsoft.Playwright;

namespace HraBot.Tests.E2E;

public class ResponseTests : PageTestBase
{
    [Test]
    public async Task WhenUserAsksQuestion_AgentResponseShouldShow()
    {
        await this.PageContext.Page.GotoAsync(
            "/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
        );

        // Assert
        await this.Expect(this.PageContext.Page).ToHaveTitleAsync("hrabot");
    }
}
