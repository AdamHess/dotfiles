namespace AccountDeduplication.CsvModels;

public class DataLoadModel
{

    public required string Id { get; set; }
    public string Name { get; set; }
    public string BillingAddress { get; set; }
    public string ShippingAddress { get; set; }
    public string OtherOrgName { get; set; }

    public bool AnyNonNullVals()
    {
        return Name != null || BillingAddress != null || ShippingAddress != null || OtherOrgName != null;
    }

}