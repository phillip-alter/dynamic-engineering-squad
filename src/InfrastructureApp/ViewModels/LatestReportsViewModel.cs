using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels
{
    // Holds all reports for the Latest Reports page
    public class LatestReportsViewModel
    {
        // Directly use the domain model for display
        public List<ReportIssue> Reports { get; set; } = new();
    }
}

