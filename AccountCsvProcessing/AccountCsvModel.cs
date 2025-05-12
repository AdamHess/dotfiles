using CsvHelper.Configuration.Attributes;

public class AccountCsvModel
{
    public string Id { get; set; }
    public string Name { get; set; }

    [Name("BillingAddress.street")] public string BillingStreet { get; set; }
}