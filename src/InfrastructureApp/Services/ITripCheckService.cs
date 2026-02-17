using System.Collections.Generic;
using System.Threading.Tasks;
using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    public interface ITripCheckService
    {
        Task<IReadOnlyList<RoadCameraViewModel>> GetCamerasAsync();

        Task<RoadCameraViewModel?> GetCameraByIdAsync(string id);
    }
}