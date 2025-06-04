namespace AccountDeduplication.DAL
{
    public class ProcessingStatus
    {
        public string GroupId { get; set; }
        
        public int  AccountsInGroup { get; set; }
        
        public DateTimeOffset ProcessedAt { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }
}
