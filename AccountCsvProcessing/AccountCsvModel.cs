using CsvHelper.Configuration.Attributes;

namespace CsvProcessing;

public class AccountCsvModel
{
    public string Id { get; set; }
    public string Name { get; set; }

    [Name("BillingStreet")] 
    public string BillingStreet { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    
    [Name("Record_Type_Name__c")] 
    public string RecordTypeName { get; set; }



    [Name("of_Roles__c")]
    public int NumberOfRoles { get; set; }
    
    public bool IsPerson => RecordTypeName == "Person Account";
}