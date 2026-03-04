using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Services;

public class UserService : IUserService
{
    private readonly UserManager<Users> _userManager;

    public UserService(UserManager<Users> userManager) => _userManager = userManager;

    public async Task<PaginatedList<AdminViewModel>> GetUsersWithRolesAsync(int page, int pageSize)
    {
        var query = _userManager.Users.Select(u => new AdminViewModel
        {
            UserId = u.Id,
            UserName = u.UserName,
            Email = u.Email
        });

        var pagedUsers = await PaginatedList<AdminViewModel>.CreateAsync(query, page, pageSize);
        
        foreach (var user in pagedUsers)
        {
            var identityUser = await _userManager.FindByIdAsync(user.UserId);
            user.Roles = (await _userManager.GetRolesAsync(identityUser)).ToList();
        }

        return pagedUsers;
    }
}