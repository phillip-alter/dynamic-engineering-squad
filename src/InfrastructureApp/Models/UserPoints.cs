using System;

namespace InfrastructureApp.Models
{
    public class UserPoints
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int CurrentPoints { get; set; } = 0;
        public int LifetimePoints { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
