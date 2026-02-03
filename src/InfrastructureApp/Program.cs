using InfrastructureApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


//Leaderboard
builder.Services.AddSingleton<ILeaderboardRepository, LeaderboardRepositoryInMemory>();
builder.Services.AddSingleton<LeaderboardService>();


var app = builder.Build();

Console.WriteLine("DI LeaderboardService registered? " + (app.Services.GetService<InfrastructureApp.Services.LeaderboardService>( )!= null));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();