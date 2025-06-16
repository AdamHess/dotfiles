namespace AccountDeduplication.DAL;

public class Account
{
    public string Id { get; set; }
    public string Name { get; set; }


    public string BillingStreet { get; set; }

    
    public string ShippingStreet { get; set; }
    
    public string RecordTypeName { get; set; }
    
    public string OtherOrgName { get; set; }

    
    public int NumberOfRoles { get; set; }
    
    
    public string NPI { get; set; }
    public string ShippingState { get; set; }
    public string BillingState { get; set; }
    public string ShippingCity { get; set; }
    public string BillingCity { get; set; }
    
    public string Grouping { get; set; }
    
    public IList<GroupPair> GroupPairs { get; set; } = new List<GroupPair>();

}
