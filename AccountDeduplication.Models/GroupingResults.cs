namespace AccountDeduplication.CsvModels;

public class GroupingResults
{
    public string GroupAccountId { get; set; }
    public string AccountId { get; set; }
    
    public string Name { get; set; }
    public string Street { get; set; }
    public bool IsGroupLeader { get; set; }
    public bool IsForcedGroupLeader { get; set; }
    public string NPI { get; set; }
    
}