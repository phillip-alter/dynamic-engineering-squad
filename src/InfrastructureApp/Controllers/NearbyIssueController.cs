using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    public class NearbyController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Just renders the map page; JS will fetch nearby reports.
            return View();
        }
    }
}