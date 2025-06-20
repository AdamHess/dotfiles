using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL.EF;
using AccountDeduplication.DAL.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.LoadDatabase;

public class Program
{
    static async Task Main()
    {
        await LoadDatabaseWithAccounts();
    }

    public static async Task LoadDatabaseWithAccounts()
    {
        await InitializeDb();
        var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
        Console.WriteLine("Loading Csv");
        var accounts = CsvBinder.LoadCsv<AccountCsvModel>(inputFile).Select(ToAccount).ToList();
        Console.WriteLine("Assigning Group Key");
        AccountGroupAssigner.AssignGroupKeys(accounts);
        Console.WriteLine("Save to Db");
        await SaveOrUpdateAccountsToDb(accounts);

    }

    private static async Task SaveOrUpdateAccountsToDb(List<Account> accounts)
    {
        await using var db = new AccountDedupeDb();

        await db.BulkInsertOrUpdateAsync(accounts);
    }


    private static async Task InitializeDb()
    {
        await using var db = new AccountDedupeDb();
        await db.Database.MigrateAsync();
    }
    public static Account ToAccount(AccountCsvModel csvModel)
    {
        var (billingHouse, billingStreet, billingUnit) = AddressParser.GetAddressParts(csvModel.BillingStreet);
        var (shippingHouse, shippingStreet, shippingUnit) = AddressParser.GetAddressParts(csvModel.ShippingStreet);

        return new Account
        {
            Id = csvModel.Id,
            Name = csvModel.Name,
            BillingStreetRaw = csvModel.BillingStreet,
            BillingStreetNormalized = AddressParser.NormalizeAddress(csvModel.BillingStreet),
            BillingHouseNumber = billingHouse,
            BillingStreet = billingStreet,
            BillingUnit = billingUnit,
            ShippingStreetRaw = csvModel.ShippingStreet,
            ShippingStreetNormalized = AddressParser.NormalizeAddress(csvModel.ShippingStreet),
            ShippingHouseNumber = shippingHouse,
            ShippingStreet = shippingStreet,
            ShippingUnit = shippingUnit,
            RecordTypeName = csvModel.RecordTypeName,
            OtherOrgName = csvModel.OtherOrgName,
            NumberOfRoles = csvModel.NumberOfRoles,
            NPI = csvModel.NPI,
            ShippingState = csvModel.ShippingState,
            BillingState = csvModel.BillingState,
            ShippingCity = csvModel.ShippingCity,
            BillingCity = csvModel.BillingCity,
            ShippingPostalCode = csvModel.ShippingPostalCode,
            BillingPostalCode = csvModel.BillingPostalCode
        };
    }
}

