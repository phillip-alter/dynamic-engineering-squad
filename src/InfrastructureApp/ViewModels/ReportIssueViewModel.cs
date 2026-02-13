using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace InfrastructureApp.ViewModels
{
    //ViewModel = form fields + validation for UI
    public class ReportIssueViewModel
    {
        [Required(ErrorMessage = "Please enter a description.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        //replaced URL with photo upload
        [Required(ErrorMessage = "Please upload a photo of the damage.")]
        [Display(Name = "Photo")]
        public IFormFile? Photo {get; set;}

        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; }
    }
}