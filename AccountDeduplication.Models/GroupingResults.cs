using System.ComponentModel.DataAnnotations.Schema;

namespace AccountDeduplication.CsvModels;

public class GroupingResults
{
    public string GroupAccountId { get; set; }
    public string AccountId { get; set; }
    
    public string Name { get; set; }
    public string Street { get; set; }
    public bool IsGroupLeader { get; set; }
    [NotMapped]
    public bool IsForcedGroupLeader { get; set; }
    public string NPI { get; set; }
    
}