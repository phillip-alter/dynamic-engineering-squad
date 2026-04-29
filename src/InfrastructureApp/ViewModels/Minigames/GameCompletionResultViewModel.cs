using System;
using System.Collections.Generic;

namespace InfrastructureApp.ViewModels.Minigames
{
    public class GameCompletionResultViewModel
    {
        public string GameKey { get; init; } = string.Empty;

        public int AwardedPoints { get; init; }

        public int CurrentPoints { get; init; }

        public IReadOnlyList<string> Symbols { get; init; } = Array.Empty<string>();

        public bool IsWinningSpin { get; init; }

        public string ResultLabel { get; init; } = string.Empty;

        public int DailyPointsEarned { get; init; }

        public int DailyPointsLimit { get; init; }

        public bool HasReachedDailyLimit { get; init; }
    }
}
