using AccountDeduplication.DAL.EF;
using AccountDeduplication.DAL.Models;

namespace AccountDeduplication.LoadDatabase
{
    public static class AccountGroupAssigner
    {

        public static void AssignGroupKeys(IEnumerable<Account> accounts)
        {
            foreach (var account in accounts)
            {
                account.GroupingCityState = CityStateBlocker.GetGroupingKey(
                    account.BillingHouseNumber ?? account.ShippingHouseNumber,
                    account.BillingCity ?? account.ShippingCity,
                    account.BillingState ?? account.ShippingState);
            }

            //var nullGroupAccounts = accounts
            //    .Where(m => m.GroupingCityState == null && !string.IsNullOrWhiteSpace(m.BillingCity ?? m.BillingState))
            //    .ToList();
            //Console.WriteLine($"Guessing State by best prefix match ({nullGroupAccounts.Count} Accounts)");
            ////AssignGroupingWhenStateIsUnknown(nullGroupAccounts, accounts);
            //Console.WriteLine($"Unable to assign groups to:  {nullGroupAccounts.Count(m => string.IsNullOrWhiteSpace(m.GroupingCityState))}");
        }
        /// <summary>
        /// Takes a best guess at what city it belongs to based on the city prefix used as the grouping key (finds an existing grouping with the most matches).
        /// </summary>
        /// <param name="nullGroupAccounts"></param>
        /// <param name="accounts"></param>
        private static void AssignGroupingWhenStateIsUnknown(List<Account> nullGroupAccounts, List<Account> accounts)
        {



            foreach (var account in nullGroupAccounts)
            {
                using var db = new AccountDedupeDb();
                var groupingKeyPrefix = CityStateBlocker.GetGroupingPair(account.BillingCity ?? account.ShippingCity);
                var possibleState = db.Accounts.Where(m =>
                    (m.BillingCity ?? m.ShippingCity) == (account.BillingCity ?? account.BillingState))
                    .GroupBy(m => (m.BillingCity ?? m.ShippingCity))
                    .OrderByDescending(m => m.Count())
                    .Select(m => m.Key)
                    .FirstOrDefault();
                if (possibleState != null)
                {
                    account.GroupingCityState = possibleState;
                }

            }
        }
    }
}
