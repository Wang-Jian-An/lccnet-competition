namespace lccnet_competition.Models
{
    public class Leaderboard
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public double Score { get; set; }
        public int SubmissionCount { get; set; }
        public DateTime? LastSubmissionDateTime { get; set; }

    }
}
