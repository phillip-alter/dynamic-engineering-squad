namespace InfrastructureApp.ViewModels
{
    // Holds all reports for the Latest Reports page
    public class LatestReportsViewModel
    {
        // List of reports to display
        public List<LatestReportItemViewModel> Reports { get; set; } = new();
    }

    // Represents one single report
    public class LatestReportItemViewModel
    {
        public int Id { get; set; }                   // Report ID

        public string Description { get; set; } = ""; // What the report is about

        public string Status { get; set; } = "";      // Current report status

        public DateTime CreatedAt { get; set; }       // When the report was created
    }
}
