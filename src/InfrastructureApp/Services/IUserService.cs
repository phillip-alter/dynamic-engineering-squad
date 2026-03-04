using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;

namespace InfrastructureApp.Services;

public interface IUserService
{
    public Task<PaginatedList<AdminViewModel>>
        GetUsersWithRolesAsync(int page, int pageSize);
}