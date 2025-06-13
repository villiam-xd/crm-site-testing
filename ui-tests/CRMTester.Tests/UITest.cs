using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace CRMTester.Tests;

[TestClass]
public class UITest : PageTest
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IBrowserContext _browserContext;
    private IPage _page;

    [TestInitialize]
    public async Task Setup()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 100
        });
        _browserContext = await _browser.NewContextAsync();
        _page = await _browserContext.NewPageAsync();
    }
    [TestCleanup]
    public async Task Cleanup()
    {
        await _browserContext.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    // Metod för att logga in (Tar in inloggningsuppgifter som parameter)
    public async Task Login(string email, string password)
    {
        // Går till startsidan och klickar på Login
        await _page.GotoAsync("http://localhost:5173/");
        await _page.GetByText("Login").ClickAsync();

        // Fyller i inloggningsuppgifter och loggar in
        await _page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email);
        await _page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
    }

    // Logga in som Admin
    public async Task LoginAsAdmin()
    {
        await Login("m@email.com", "abc123");
    }

    // Logga in som användare
    public async Task LoginAsUser()
    {
        await Login("no@email.com", "abc123");
    }

    // Testar att logga in och logga ut och om man hamnar på mainpagen efteråt
    [TestMethod]
    public async Task LoginAndLogout()
    {
        await LoginAsAdmin();

        // Loggar ut
        await _page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();

        // Kollar vilken URL man hamnar på efter att ha loggat ut
        await Expect(_page).ToHaveURLAsync("http://localhost:5173/");
    }

    // Testar ändra status på issues som admin
    [TestMethod]
    public async Task ChangeIssueStateAsAdmin()
    {
        await LoginAsAdmin();

        // Går till issuestabben
        await _page.GetByText("Issues").ClickAsync();

        // Ändrar status och sparar (Återupprepas för de olika möjliga statuslägena)
        await _page.Locator("button.subjectEditButton").First.ClickAsync();
        await _page.Locator("select.stateSelect").First.SelectOptionAsync("OPEN");
        await _page.Locator("button.stateUpdateButton").First.ClickAsync();

        await _page.Locator("button.subjectEditButton").First.ClickAsync();
        await _page.Locator("select.stateSelect").First.SelectOptionAsync("CLOSED");
        await _page.Locator("button.stateUpdateButton").First.ClickAsync();

        await _page.Locator("button.subjectEditButton").First.ClickAsync();
        await _page.Locator("select.stateSelect").First.SelectOptionAsync("NEW");
        await _page.Locator("button.stateUpdateButton").First.ClickAsync();
    }

    [TestMethod]
    public async Task AddAndVerifyNewSubject()
    {
        await LoginAsAdmin();

        // Går till Form Subjects tabben
        await _page.GetByText("Form Subjects").ClickAsync();

        // Skapar nytt subject och sparar
        await _page.GetByRole(AriaRole.Button, new() { Name = "New Subject" }).ClickAsync();
        await _page.GetByRole(AriaRole.Textbox, new() { Name = "New Subject" }).FillAsync("Test Subject");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Verifierar att den nya finns i listan
        await Expect(_page.GetByText("Test Subject")).ToBeVisibleAsync();
    }
}