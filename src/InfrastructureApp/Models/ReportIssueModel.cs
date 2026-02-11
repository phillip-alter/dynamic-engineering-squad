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
        public decimal? Latitude {get; set;}

        //DECIMAL (9,6)
        [Range(-180, 180)]
        public decimal? Longitude {get; set;}

        //NVARCHAR(450) (URL to blob)
        [MaxLength(450)]
        public string? ImageUrl {get; set;}
    }
}