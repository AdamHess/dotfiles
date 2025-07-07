using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL.Models;
using AccountDeduplication.RecordLoggers;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.LoadDatabase;

public class LoaderAndProcessor(Func<DbContext> contextFactory)
{

    public async Task LoadDatabaseAndSaveAccounts(string inputFile, string groupingContains = null, string outputFile = null)
    {
        Console.WriteLine("Loading Csv");

        var accounts = CsvBinder.LoadCsv<AccountCsvModel>(inputFile).ToList();

        Console.WriteLine($"Total Accounts {accounts.Count}");
        List<Account> accountsToSave;
        if (groupingContains != null)
        {
            accountsToSave = accounts
                .AsParallel()
                .Select(ToAccount)
                .Where(m => !string.IsNullOrWhiteSpace(m.GroupingCityState) &&
                            m.GroupingCityState.Contains(groupingContains))
                .ToList();
        }
        else
        {
            accountsToSave = accounts
                .AsParallel()
                .Select(ToAccount)
                .ToList();
        }


        Console.WriteLine($"Accounts to Save {accountsToSave.Count}");
        await SaveOrUpdateAccountsToDb(accountsToSave);
        if (outputFile != null)
        {
            await using var csvLogger = new CsvLogger<AccountCsvModel>(outputFile);
            await csvLogger.AddEntriesAsync(accounts.Where(m => accountsToSave.Any(n => n.Id == m.Id)));
        }
    }


    private async Task SaveOrUpdateAccountsToDb(List<Account> accounts)
    {
        await using var db = contextFactory();
        await db.BulkInsertOrUpdateAsync(accounts);
    }



    public static Account ToAccount(AccountCsvModel csvModel)
    {
        var (billingHouse, billingStreet, billingUnit) = AddressParser.GetAddressParts(csvModel.BillingStreet);
        var (shippingHouse, shippingStreet, shippingUnit) = AddressParser.GetAddressParts(csvModel.ShippingStreet);



        var account = new Account
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
            BillingPostalCode = csvModel.BillingPostalCode,
            GroupingCityState = CityStateBlocker.GetGroupingKey(csvModel.RecordTypeName,
                billingHouse,
                csvModel.BillingCity ?? csvModel.ShippingCity,
                csvModel.BillingState ?? csvModel.ShippingStreet,
                billingUnit),

            FirstName = csvModel.FirstName,
            LastName = csvModel.LastName

        };
        return account;
    }
}

