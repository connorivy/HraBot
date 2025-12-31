using Microsoft.Playwright;
using TUnit.Playwright;

namespace HraBot.Tests.E2E;

public abstract class PageTestBase : ContextTest
{
    public PageContext PageContext { get; private set; } = null!;
    public IPage Page => this.PageContext.Page;
    protected virtual bool SaveSnapshotsOnSuccess => false;

    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var options = base.ContextOptions(testContext);
        options.BaseURL = this.FrontendAddressOverride ?? SetupTestsE2E.FrontendAddress;
        options.IgnoreHTTPSErrors = true;
        return options;
    }

    protected virtual string? FrontendAddressOverride { get; }

    [Before(HookType.Test, "", 0)]
    public async Task PageSetup(TestContext testContext)
    {
        if (this.Context == null)
        {
            throw new InvalidOperationException(
                $"Browser context is not initialized. This may indicate that {nameof(ContextTest)}.{nameof(ContextSetup)} did not execute properly."
            );
        }

        this.PageContext = await PageContext
            .CreateAsync(
                this.Browser!,
                this.Context,
                await this.Context.NewPageAsync().ConfigureAwait(false),
                testContext.Metadata.TestName,
                this.SaveSnapshotsOnSuccess
            )
            .ConfigureAwait(false);
    }

    [After(HookType.Test, "", 0)]
    public async Task PageTeardown(TestContext testContext)
    {
        if (this.PageContext != null)
        {
            Console.WriteLine($"Disposing PageContext for test {testContext.Metadata.TestName}");
            await this.PageContext.DisposeAsync(testContext).ConfigureAwait(false);
        }
    }
}

public sealed class PageContext
{
    private PageContext(
        IBrowser browser,
        IBrowserContext context,
        IPage page,
        string testName,
        bool saveSnapshotsOnSuccess = false
    )
    {
        this.Browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Page = page ?? throw new ArgumentNullException(nameof(page));
        this.testName = testName;
        this.saveSnapshotsOnSuccess = saveSnapshotsOnSuccess;
    }

    public IBrowser Browser { get; }
    public IBrowserContext Context { get; }
    public IPage Page { get; }
    private readonly string testName;
    private readonly bool saveSnapshotsOnSuccess;

    public static async Task<PageContext> CreateAsync(
        IBrowser browser,
        IBrowserContext context,
        IPage page,
        string testName,
        bool saveSnapshotsOnSuccess = false
    )
    {
        var contextInstance = new PageContext(
            browser,
            context,
            page,
            testName,
            saveSnapshotsOnSuccess
        );
        await contextInstance.Context.Tracing.StartAsync(
            new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            }
        );
        return contextInstance;
    }

    public async ValueTask DisposeAsync(TestContext testContext)
    {
        bool testFailed =
            testContext.Execution.Result?.State is not TestState.Passed and not TestState.Skipped;
        string? path;
        if (testFailed || this.saveSnapshotsOnSuccess)
        {
            path = $"trace-{this.testName}.zip";
            Console.WriteLine(
                $"Test {this.testName} failed. View trace info with this command:\n"
                    + $"    npx playwright show-trace {Path.Combine(AppContext.BaseDirectory, path)} --host 0.0.0.0"
            );
        }
        else
        {
            path = null;
        }
        await this.Context.Tracing.StopAsync(new() { Path = path }).ConfigureAwait(false);
        await this.Page.CloseAsync().ConfigureAwait(false);
        // await this.Context.CloseAsync().ConfigureAwait(false);
        // await this.Browser.CloseAsync().ConfigureAwait(false);
    }
}
