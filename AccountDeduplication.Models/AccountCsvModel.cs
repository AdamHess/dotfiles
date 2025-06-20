namespace AccountDeduplication.CsvModels;

public class AccountCsvModel
{



    public required string Id { get; set; }
    public string? Name { get; set; }


    public string? ShippingStreet { get; set; }
    public string? BillingStreet { get; set; }

    public string? RecordTypeName { get; set; }

    public string? OtherOrgName { get; set; }


    public int NumberOfRoles { get; set; }
    public string? NPI { get; set; }
    public string? ShippingState { get; set; }
    public string? BillingState { get; set; }
    public string? ShippingCity { get; set; }
    public string? BillingCity { get; set; }

    public string? ShippingPostalCode { get; set; }
    public string? BillingPostalCode { get; set; }

}
