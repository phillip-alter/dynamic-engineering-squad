using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using InfrastructureApp.Data;

namespace InfrastructureApp.Controllers;

public class LeaderboardController : Controller
{
    private readonly LeaderboardService _service;

     public LeaderboardController(LeaderboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int top = 25)
    {
        var entries = await _service.GetTopAsync(top);

        var vm = new LeaderboardIndexViewModel
        {
            Entries = entries,
            TopN = top <= 0 ? 25 : top
        };

        return View(vm);
    }
}
