using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InfrastructureApp_Tests;

[TestFixture]
public class AuthenticationIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task
        UnauthedUser_AccessingDashboard_RedirectsToLogin()
    {
        var protectedUrl = "/Dashboard";
        
        var response = _client.GetAsync(protectedUrl).Result;
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Redirect));

        var redirectLoc = response.Headers.Location?.ToString();
        Assert.That(redirectLoc,
            Does.Contain("/Login"));
    }
    
    [Test]
    public async Task
        UnauthedUser_AccessingReportIssues_RedirectsToLogin()
    {
        var protectedUrl = "/ReportIssue/ReportIssue";
        
        var response = _client.GetAsync(protectedUrl).Result;
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Redirect));

        var redirectLoc = response.Headers.Location?.ToString();
        Assert.That(redirectLoc,
            Does.Contain("/Login"));
    }
}