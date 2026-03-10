using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace InfrastructureApp.Models
{
    //This maps to the ERD's Report Table
    public class ReportIssue
    {
        //PK Identity
        public int Id { get; set; }

        //NVARCHAR(MAX)
        [Required(ErrorMessage = "Please enter a description.")]
        [StringLength(300, ErrorMessage = "Description must be 300 characters or less.")]
        [MaxLength(300, ErrorMessage = "Description must be 300 characters or less.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        //NVARCHAR(50): Pending, Approved, Rejected
        [Required]
        [MaxLength(50)]

        public string Status { get; set; } = "Approved";

        //DATETIME2
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //FK to ApsNetUser.Id (nvarchar(450))
        [Required]
        public string UserId { get; set; } = "";

        //DECIMAL(9,6)
        [Required(ErrorMessage = "Please select a location on the map to populate Latitude.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal? Latitude { get; set; }

        //DECIMAL (9,6)
        [Required(ErrorMessage = "Please select a location on the map to populate Longitude.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal? Longitude { get; set; }

        //NVARCHAR(450) (URL to blob)
        [MaxLength(450)]
        public string? ImageUrl { get; set; }

        // ----------------------------------------------------
        // UI-only properties for form submission
        // Not mapped to the database
        // ----------------------------------------------------

        [Required(ErrorMessage = "Please upload a photo of the damage.")]
        [Display(Name = "Photo")]
        [NotMapped]
        public IFormFile? Photo { get; set; }

        [NotMapped]
        public string? CameraId { get; set; }

        [NotMapped]
        public string? CameraImageUrl { get; set; }


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

        // Filters by keyword in Description (case-insensitive)
        public static IQueryable<ReportIssue> FilterByDescription(IQueryable<ReportIssue> query, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return query;
            }

            keyword = keyword.Trim().ToLower();

            // Case-insensitive matching
            return query.Where(r => r.Description != null && r.Description.ToLower().Contains(keyword));
        }

        // Applies sorting based on selected sort option
        // SCRUM-86 ADDED: allows sorting newest or oldest without putting logic in controller
        public static IQueryable<ReportIssue> ApplyDateSort(IQueryable<ReportIssue> query, string? sort)
        {
            // SCRUM-86 ADDED: oldest-first option
            if (!string.IsNullOrWhiteSpace(sort) && sort.Trim().ToLower() == "oldest")
            {
                return query.OrderBy(r => r.CreatedAt);
            }

            // default behavior (newest-first)
            return query.OrderByDescending(r => r.CreatedAt);
        }

    }
}