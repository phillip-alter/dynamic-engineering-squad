using System;

namespace InfrastructureApp.Models
{ 

    //UserPoints is a persistence entity that represents a uer's 
    //point totals within the system
    //This class is mapped directly to a database table via EF Core.
    //It stores both the user's current score (Used for leaderboards)
    //and their lifetime score 
    public class UserPoints
    {
        //Primary key for the UserPoints table
        //EF Core will treat this as an identity column by convention
        public int Id { get; set; }

        //Foreign key linking this record to an ASP.NET Core Identity user.
        //This value corresponds to IdentityUser.Id.

        //Marked non nullable because every UserPoints record must belong
        //to exactly one autheticated user
        public string UserId { get; set; } = null!;

        //Users acttive point total
        //Initialized to 0 to ensure a known default state for new users.
        //A database level default is also configures as a safety net.
        public int CurrentPoints { get; set; } = 0;

        //Tracks the total points earned by the user over time.
        //This value should only increase over time
        public int LifetimePoints { get; set; } = 0;

        //Stored in UTC to avoid time zone issues and ensure consistency
        //Across environments, services, and geo regions.
        //Although initialized here, the db also enforces a default value
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
