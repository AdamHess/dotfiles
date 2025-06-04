using AccountDeduplication.DAL;
using CsvHelper.Configuration;

namespace CsvProcessing;

public class AccountCsvModelMap : ClassMap<AccountDeduplication.DAL.Account>
{
    public AccountCsvModelMap()
    {
        
        Map(m => m.Id);
        Map(m => m.Name);
        Map(m => m.BillingStreet).Name("BillingStreet");
        Map(m => m.ShippingStreet).Name("ShippingStreet");
        Map(m => m.RecordTypeName).Name("Record_Type_Name__c");
        Map(m => m.OtherOrgName).Name("Account_Name_DBA_2__c");
        Map(m => m.NumberOfRoles).Name("of_Roles__c");
        Map(m => m.NPI).Name("NPI__c");
        Map(m => m.BillingCity);
        Map(m => m.BillingState);
        Map(m => m.ShippingCity);
        Map(m => m.ShippingState);
        // IsPerson is computed, so not mapped
    }
}