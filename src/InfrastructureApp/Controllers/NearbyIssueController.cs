using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    // MVC Controller responsible for rendering the
    // "Nearby Issues Map" page
    public class NearbyIssueController : Controller
    {
        //   /NearbyIssue/Index
        [HttpGet]
        public IActionResult Index()
        {
            // Just renders the map page; JS will fetch nearby reports.
            return View();
        }
    }
}