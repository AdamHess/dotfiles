using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL.Models;
using AccountDeduplication.Parquet;
using AccountDeduplication.RecordLoggers;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.LoadDatabase;

public class LoaderAndProcessor(Func<DbContext> contextFactory)
{
    private DbLogger<Account> DbLogger { get; set; } 
    public async Task LoadDatabaseAndSaveAccounts(string inputFile, 
        string groupingContains = null, 
        string outputFile = null)
    {
        DbLogger = new DbLogger<Account>(contextFactory);
        List<Account> accountsToSave = [];
        if (Path.GetExtension(inputFile) == ".csv")
        {
            accountsToSave = ProcessFileAsCSV(inputFile, groupingContains);   
        }
        else if (Path.GetExtension(inputFile) == ".parquet")
        {
            var binder = new ParquetBinder<AccountParquetModel>();
            accountsToSave =  binder.ReadParallel(inputFile).AsParallel()
                .SelectMany(m => m)
                .Select(ToAccountFromParquet)
                .ToList();
        }
        
        // await SaveOrUpdateAccountsToDb(accountsToSave);

        // Console.WriteLine($"Accounts to Save {accountsToSave.Count}");
        // await SaveOrUpdateAccountsToDb(accountsToSave);
        // if (outputFile != null)
        // {
        //     await using var csvLogger = new CsvLogger<AccountCsvModel>(outputFile);
        //     await csvLogger.AddEntriesAsync(accounts.Where(m => accountsToSave.Any(n => n.Id == m.Id)));
        // }
        await DbLogger.DisposeAsync();
    }

    private Account ToAccountFromParquet(AccountParquetModel parquetModel)
    {
        var (billingHouse, billingStreet, billingUnit) = AddressParser.GetAddressParts(parquetModel.BillingStreet);
        var (shippingHouse, shippingStreet, shippingUnit) = AddressParser.GetAddressParts(parquetModel.ShippingStreet);



        var account = new Account
        {
            Id = parquetModel.Id,
            Name = parquetModel.Name,
            BillingStreetRaw = parquetModel.BillingStreet,
            BillingStreetNormalized = AddressParser.NormalizeAddress(parquetModel.BillingStreet),
            BillingHouseNumber = billingHouse,
            BillingStreet = billingStreet,
            BillingUnit = billingUnit,
            ShippingStreetRaw = parquetModel.ShippingStreet,
            ShippingStreetNormalized = AddressParser.NormalizeAddress(parquetModel.ShippingStreet),
            ShippingHouseNumber = shippingHouse,
            ShippingStreet = shippingStreet,
            ShippingUnit = shippingUnit,
            RecordTypeName = parquetModel.RecordTypeName,
            OtherOrgName = parquetModel.AccountNameDba2,
            NumberOfRoles = parquetModel.OfRoles,
            NPI = parquetModel.Npi,
            ShippingState = parquetModel.ShippingState,
            BillingState = parquetModel.BillingState,
            ShippingCity = parquetModel.ShippingCity,
            BillingCity = parquetModel.BillingCity,
            ShippingPostalCode = parquetModel.ShippingPostalCode,
            BillingPostalCode = parquetModel.BillingPostalCode,
            GroupingCityState = CityStateBlocker.GetGroupingKey(parquetModel.RecordTypeName,
                billingHouse,
                parquetModel.BillingCity ?? parquetModel.ShippingCity,
                parquetModel.BillingState ?? parquetModel.ShippingStreet,
                billingUnit),

            FirstName = parquetModel.CaseFirstName,
            LastName = parquetModel.CaseLastName,

        };
        DbLogger.AddEntryAsync(account).GetAwaiter().GetResult();
        return account;    
    }

    private static List<Account> ProcessFileAsCSV(string inputFile, string groupingContains)
    {
        Console.WriteLine("Loading Csv");

        var accounts = CsvBinder.LoadCsv<AccountCsvModel>(inputFile).ToList();

        Console.WriteLine($"Total Accounts {accounts.Count}");
        List<Account> accountsToSave;
        if (groupingContains != null)
        {
            accountsToSave = accounts
                .AsParallel()
                .Select(ToAccountFromCsv)
                .Where(m => !string.IsNullOrWhiteSpace(m.GroupingCityState) &&
                            m.GroupingCityState.Contains(groupingContains))
                .ToList();
        }
        else
        {
            accountsToSave = accounts
                .AsParallel()
                .Select(ToAccountFromCsv)
                .ToList();
        }

        return accountsToSave;
    }


    private async Task SaveOrUpdateAccountsToDb(List<Account> accounts)
    {
        await using var db = contextFactory();
        await db.BulkInsertOrUpdateAsync(accounts);
    }



    public static Account ToAccountFromCsv(AccountCsvModel csvModel)
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

