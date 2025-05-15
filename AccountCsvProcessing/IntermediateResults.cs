public class FinalResults: IntermediateResults
{
    public string OldName { get; set; }
    public string OldBillingAddress { get; set; }
    public string NewName { get; set; }
    public string NewBillingAddress { get; set; }
    
}

public class IntermediateResults
{
    public string AccountId { get; set; }
    public string MatchToAccountId { get; set; }
    public double MatchPercentage { get; set; }
    public double RoleCount { get; set; }
}