using Microsoft.AspNetCore.Identity;
using InfrastructureApp.Services;


namespace InfrastructureApp.Models
{
    public class Users : IdentityUser
    {
        public string? AvatarKey { get; set; } // Added line to db so that AvatarKey is reachable
    }
}
