using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Models
{
    public class Users : IdentityUser
    {
        public Users(string userName, string email)
        {
            NormalizedUserName = userName.ToUpper();
            NormalizedEmail = email.ToUpper();
        }
    }
}
