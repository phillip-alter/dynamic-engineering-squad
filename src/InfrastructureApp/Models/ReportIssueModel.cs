using System;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureApp.Models
{
    //This maps to the ERD's Report Table
    public class ReportIssue
    {
        //PK Identity
        public int Id {get; set;}

        //NVARCHAR(MAX)
        [Required]
        public string Description {get; set;} = "";

        //NVARCHAR(50): Pending, Approved, Rejected
        [Required]
        [MaxLength(50)]
        public string Status {get; set;} = "Pending";

        //DATETIME2
        public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

        //FK to ApsNetUser.Id (nvarchar(450))
        [Required]
        public string UserId {get; set; } = "";

        //DECIMAL(9,6)
        [Range(-90, 90)]
        public decimal Latitude {get; set;}

        //DECIMAL (9,6)
        [Range(-180, 180)]
        public decimal Longitude {get; set;}

        //NVARCHAR(450) (URL to blob)
        [MaxLength(450)]
        public string? ImageUrl {get; set;}

        // ----------------------------------------------------
        // Report query helpers
        // Keep filtering and sorting logic here instead of the controller
        // Moved it to here because this keeps controllers simple and separates data logic from UI logic
        // ----------------------------------------------------

        // Determines which reports should be visible to the user
        public static IQueryable<ReportIssue> VisibleToUser(IQueryable<ReportIssue> query, bool isAdmin)
        {
            // Admin sees all reports; others see only approved ones
            if (isAdmin)
            {
                return query;
            } 

            return query.Where(r => r.Status == "Approved");
        }

        // Orders reports so the newest appear first
        public static IQueryable<ReportIssue> OrderLatestFirst(IQueryable<ReportIssue> query)
        {
            // Sort by creation date (newest first)
            return query.OrderByDescending(r => r.CreatedAt);
        }

    }
}