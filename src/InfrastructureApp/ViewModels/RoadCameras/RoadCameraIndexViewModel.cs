using System.Collections.Generic;

namespace InfrastructureApp.ViewModels
{
    public class RoadCameraIndexViewModel
    {
        public List<RoadCameraViewModel> Cameras { get; set; } = new();

        public string? ErrorMessage { get; set; }
    }
}