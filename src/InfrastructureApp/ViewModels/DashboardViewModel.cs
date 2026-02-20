namespace InfrastructureApp.ViewModels
{
    // Holds the data needed to display the Dashboard page
    public class DashboardViewModel
    {
        // User's display name
        public string Username { get; set; } = "";

        // User's email address
        public string Email { get; set; } = "";

        // Total number of reports submitted by the user
        public int ReportsSubmitted { get; set; }

        // Current points earned by the user
        public int Points { get; set; }
    }
}