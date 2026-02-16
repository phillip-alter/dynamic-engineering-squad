/* Defines the business operations for reporting infrastructure issues, 
separating controller logic from database access and enforcing application rules through a dedicated service layer.
The controller interacts with this interface instead of directly
accessing the database or repository.*/

using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    public interface IReportIssueService
    {
        //Creates a new report using the data from the ViewModel and the authenticated user's Id. Returns the Id of the newly created report.
        Task<int> CreateAsync(ReportIssueViewModel vm, string userId);

        // Retrieves a report by its Id. Returns null if the report does not exist.
        Task<ReportIssue?> GetByIdAsync(int id);
    }
}
