using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Authorization;
using InfrastructureApp.Services;

namespace InfrastructureApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    // SCRUM-103 ADDED:
    // Repository used to load recent reports for the Home page
    private readonly IReportIssueRepository? _repo;

    // Single constructor:
    // old tests can still pass just logger,
    // app can pass logger + repository
    public HomeController(ILogger<HomeController> logger, IReportIssueRepository? repo = null)
    {
        _logger = logger;
        _repo = repo;
    }

    public async Task<IActionResult> Index()
    {
        // SCRUM-103:
        // If repository is not available, return an empty list
        if (_repo == null)
        {
            return View(new List<ReportIssue>());
        }

        // SCRUM-103:
        // Admin users can see all reports, others only see approved ones
        bool isAdmin = User.IsInRole("Admin");

        // SCRUM-103:
        // Load latest reports for the Home page
        var recentReports = await _repo.GetLatestReportsAsync(isAdmin);

        // SCRUM-103:
        // Show only a small preview on the Home page
        recentReports = recentReports.Take(3).ToList();

        return View(recentReports);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult About()
    {
        return View();
    }
}