namespace AccountDeduplication.DAL.Models
{
    public class ProcessingStatus
    {
        public required string GroupId { get; set; }

        public int AccountsInGroup { get; set; }

        public DateTimeOffset ProcessedAt { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }
}
