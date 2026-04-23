using System;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class FlagService : IFlagService
    {
        private readonly ApplicationDbContext _db;

        public FlagService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<(bool Success, string Message)> FlagReportAsync(int reportId, string userId, string category)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasUserFlaggedAsync(int reportId, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
