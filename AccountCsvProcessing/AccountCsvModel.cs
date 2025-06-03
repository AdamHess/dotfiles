using CsvHelper.Configuration.Attributes;

namespace CsvProcessing;

public class AccountCsvModel
{
    public string Id { get; set; }
    public string Name { get; set; }

    [Name("BillingStreet")] 
    public string BillingStreet { get; set; }

    [Name("ShippingStreet")]
    public string ShippingStreet { get; set; }
    [Name("Record_Type_Name__c")] 
    public string RecordTypeName { get; set; }
    
    [Name("Account_Name_DBA_2__c")]
    public string OtherOrgName { get; set; }

    [Name("of_Roles__c")]
    public int NumberOfRoles { get; set; }
    
    [Name("NPI__c")]
    public string NPI { get; set; }
    
    public bool IsPerson => RecordTypeName == "Person Account";
}