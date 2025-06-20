namespace AccountDeduplication.DAL.Models;

public class Account
{


    public required string Id { get; set; }
    public string? Name { get; set; }

    public string? BillingStreetRaw { get; set; }
    public string? BillingStreetNormalized { get; set; }

    public string? BillingStreet { get; set; }
    public string? BillingHouseNumber { get; set; }

    public string? BillingUnit { get; set; }


    public string? ShippingStreetRaw { get; set; }
    public string? ShippingStreetNormalized { get; set; }

    public string? ShippingHouseNumber { get; set; }

    public string? ShippingStreet { get; set; }
    public string? ShippingUnit { get; set; }


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

    public string? GroupingCityState { get; set; }


    public virtual IList<GroupPair> GroupPairs { get; set; } = [];

}
