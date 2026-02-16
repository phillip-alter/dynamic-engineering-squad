using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels;


//Defines the data contract between the leaderboardcontroller and the Index/cshtml Raxor view


//ViewModel is intentionally read only
//It contains only the data the view needs, no business logic 
public class LeaderboardIndexViewModel
{

    //The collection of leaderboard entries to be displayed
    //IReadonly prevents accidental modification in the view layer

    //Initialized to an empty array to avoid null checks in Razor views
    public IReadOnlyList<LeaderboardEntry> Entries { get; init; } = Array.Empty<LeaderboardEntry>();

    //Allows the controller to surface non fatal issues
    //without throwing exceptions or breaking page rendering
    public string? ErrorMessage { get; init; }


    //The number of leaderboard entries requested/displayed
    //Defaults to 25 to match service level expectations
    public int TopN { get; init; } = 25;
}
