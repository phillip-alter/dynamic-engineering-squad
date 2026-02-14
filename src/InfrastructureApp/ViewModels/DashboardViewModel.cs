namespace InfrastructureApp.ViewModels
{
    public class DashboardViewModel
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public int ReportsSubmitted { get; set; }
        public int Points { get; set; }
    }
}