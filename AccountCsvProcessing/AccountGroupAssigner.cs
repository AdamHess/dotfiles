using AccountDeduplication.DAL;

namespace AccountDeduplication.CalculateMatchRates
{
    public static class AccountGroupAssigner
    {
        
        public static void AssignGroupKeys(List<Account> accounts)
        {
            foreach (var account in accounts)
            {
                account.Grouping = CityStateBlocker.GetGroupingKey(account.BillingCity ?? account.ShippingCity, account.BillingState ?? account.ShippingState);
            }

            var nullGroupAccounts = accounts
                .Where(m => m.Grouping == null && (!string.IsNullOrWhiteSpace(m.BillingCity) || !string.IsNullOrWhiteSpace(m.ShippingCity)))
                .ToList();
            Console.WriteLine($"Guessing State by best prefix match ({nullGroupAccounts.Count} Accounts)");
            AssignGroupingWhenStateIsUnknown(nullGroupAccounts, accounts);
        }
        /// <summary>
        /// Takes a best guess at what city it belongs to based on the city prefix used as the grouping key (finds an existing grouping with the most matches).
        /// </summary>
        /// <param name="nullGroupAccounts"></param>
        /// <param name="accounts"></param>
        private static void AssignGroupingWhenStateIsUnknown(List<Account> nullGroupAccounts, List<Account> accounts)
        {
            
            
            var accountGroupCount = accounts
                .Where(m => m.Grouping != null)
                .GroupBy(m => m.Grouping)
                .Select(m => new
                {
                    Key = m.Key,
                    Count = m.Count()
                })
                .OrderByDescending(m => m.Count)
                .ToList();

            foreach (var account in nullGroupAccounts)
            {
                var groupingKeyPrefix = CityStateBlocker.GetGroupingPair(account.BillingCity ?? account.BillingState);
                var bestMatch = accountGroupCount.FirstOrDefault(m => m.Key.StartsWith(groupingKeyPrefix));
                if (bestMatch != null)
                {
                    account.Grouping = bestMatch.Key;
                }
                
            }
        }
    }
}
