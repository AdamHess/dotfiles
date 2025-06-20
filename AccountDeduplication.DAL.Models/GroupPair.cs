namespace AccountDeduplication.DAL.Models
{
    public enum Phase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
        Phase4 = 4
    }

    public class GroupPair
    {
        public required string AccountId { get; set; }
        public Phase Phase { get; set; }
        public required string GroupAccountId { get; set; }
        public double MatchPercentage { get; set; }


        public virtual required Account Account { get; set; }
        public virtual required Account GroupAccount { get; set; }
    }
}
