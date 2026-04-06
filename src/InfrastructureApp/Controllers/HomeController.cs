using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Authorization;
using InfrastructureApp.Services;

namespace InfrastructureApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    // SCRUM-103: Repository used to retrieve report data for the Home page
    private readonly IReportIssueRepository _repo;

    public HomeController(ILogger<HomeController> logger, IReportIssueRepository repo)
    {
        _logger = logger;
        _repo = repo; // SCRUM-103: store repository for later use
    }

    // GET: /Home/Index
    public async Task<IActionResult> Index()
    {
        // SCRUM-103: Check if user is an Admin to determine report visibility
        bool isAdmin = User.IsInRole("Admin");

        // SCRUM-103: Retrieve latest reports from the database
        // Repository already applies visibility and sorting rules
        var recentReports = await _repo.GetLatestReportsAsync(isAdmin);

        // SCRUM-103:Limit the number of reports shown on the Home page
        // This keeps the section as a small preview instead of a full list
        recentReports = recentReports.Take(3).ToList();

        // SCRUM-103: Pass the list of reports to the Home view
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