namespace InfrastructureApp.ViewModels
{
    public class LatestReportsViewModel
    {
        public List<LatestReportItemViewModel> Reports { get; set; } = new();
    }

    public class LatestReportItemViewModel
    {
        public int Id { get; set; }
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
