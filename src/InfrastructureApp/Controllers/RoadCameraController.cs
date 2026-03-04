using System.Collections.Generic;
using System.Threading.Tasks;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;


namespace InfrastructureApp.Controllers
{
    public class RoadCameraController : Controller
    {
        private const string FriendlyApiDownMessage = "Road camera data is temporarily unavailable.";

        private readonly ITripCheckService _service;

        public RoadCameraController(ITripCheckService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var cameras = await _service.GetCamerasAsync();

            var vm = new RoadCameraIndexViewModel
            {
                Cameras = new List<RoadCameraViewModel>(cameras)
            };

            if (vm.Cameras.Count == 0)
                vm.ErrorMessage = FriendlyApiDownMessage;


            return View(vm);    
        }

        public async Task<IActionResult> Details(string id)
        {
            var camera = await _service.GetCameraByIdAsync(id);

            var vm = new RoadCameraDetailsViewModel
            {
                Camera = camera
            };

            if(camera == null)
               vm.ErrorMessage = FriendlyApiDownMessage;


            return View(vm);    
        }

        public async Task<IActionResult> RefreshImage(string id)
        {
            var cam = await _service.GetCameraByIdAsync(id);

            if(cam == null)
                return Json(new Dictionary<string, object>{{ "error", "not found" }});

            return Json(new Dictionary<string, object>
            {
                { "cameraId", cam.CameraId },
                { "imageUrl", cam.ImageUrl ?? ""},
                { "lastUpdated", cam.LastUpdated?.ToString() ?? "" }
            });    
        }
    }
}