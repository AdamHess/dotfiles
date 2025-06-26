namespace AccountDeduplication.CsvModels;

public class GroupingResults
{
    public required string GroupAccountId { get; set; }
    public required string AccountId { get; set; }

    public required string Name { get; set; }
    public required string Street { get; set; }
    public required bool IsGroupLeader { get; set; }
    public required string NPI { get; set; }

}