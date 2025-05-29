namespace CsvProcessing;

public class GroupedAccountAssignment
{
    public string GroupAccountId { get; set; }
    public string AccountId { get; set; }
    
    public string NewName { get; set; }
    
    public string OldName { get; set; }
    
    public string NewStreet { get; set;  }
    public string OldStreet { get; set; }
    public string NewFirstName { get; set; }
    public string OldFirstName { get; set; }
    public string NewLastName { get; set; }
    public string OldLastName { get; set; }
}