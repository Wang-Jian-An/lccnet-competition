using lccnet_competition.Models;

namespace lccnet_competition.ViewModels
{
    public class LeaderboardViewModel
    {
        public IEnumerable<Leaderboard>? ClassificationLeaderboard { get; set; }
        public IEnumerable<Leaderboard>? SegmentationLeaderboard { get; set; }
    }
}
