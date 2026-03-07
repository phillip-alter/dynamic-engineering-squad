using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using InfrastructureApp.Data;



namespace InfrastructureApp.Controllers;

//Controller responsible for handling requests related to the leaderboard feature.
//In MVC, controllers coodinate between services and views
public class LeaderboardController : Controller
{
    //Service is injected via dependency injection.
    //This keeps data access and business logic out of the controller.
    private readonly LeaderboardService _service;

    //Consstructor injection ensures the leaderboardservice is provided by ASP.NET Core's built in DI container.
     public LeaderboardController(LeaderboardService service)
    {
        _service = service;
    }
    //Handles HTTP GET requests to /Leaderboard or /Leaderboard/Index
    //Oprtional query parameter 'top' controls how many leaderboard entries to fetch
    [HttpGet]
    public async Task<IActionResult> Index(int topN = 25)
    {
        //Calls into the service layer to retrieve the top N Leaderboard entries
        //Async keeps the request non blocking while waiting on the db
        var entries = await _service.GetTopAsync(topN);

        //Populate a strongly typed ViewModel that the Razor view will consume.
        //This avoids passing raw entities or service models directly to the view
        var vm = new LeaderboardIndexViewModel
        {
            //The actual leaderboard records
            Entries = entries,

            //Defensive check
            //If an invalid or negative value is provided, default back to 25
            TopN = topN <= 0 ? 25 : topN
        };

        //Returns the Index.cshtml view and passes the ViewModel to it.
        //The view will be strongly typed to LeaderboardIndexViewModel.
        return View(vm);
    }
}
