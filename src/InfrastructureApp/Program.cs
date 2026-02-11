using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfrastructureApp.Services;
using Microsoft.Extensions.Options;
using InfrastructureApp.Repositories;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();




//Leaderboard
// Leaderboard (DB-backed)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<Users, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false; // until we set up email api we don't need confirmation
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepositoryEf>();
builder.Services.AddScoped<LeaderboardService>();

builder.Services.AddScoped<IReportIssueRepository, ReportIssueRepositoryEf>();
builder.Services.AddScoped<IReportIssueService, ReportIssueService>();




var app = builder.Build();

//Console.WriteLine("DI LeaderboardService registered? " + (app.Services.GetService<InfrastructureApp.Services.LeaderboardService>( )!= null));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles(); //for photo uploads

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();