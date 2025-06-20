using System.ComponentModel.DataAnnotations.Schema;

namespace AccountDeduplication.DAL.Models
{
    public class MatchRate
    {
        private const double MinimumPassValue = 0.65;
        public required string AccountId1 { get; set; }

        public required string AccountId2 { get; set; }

        public double BillingStreetMatch { get; set; }
        public double ShippingAddressMatch { get; set; }
        public double NameMatch { get; set; }

        public double OtherNameMatch { get; set; }

        public int Account1RoleCount { get; set; }
        public int Account2RoleCount { get; set; }

        [ForeignKey(nameof(AccountId1))]
        public virtual Account Account1 { get; set; } = null!;

        [ForeignKey(nameof(AccountId2))]
        public virtual Account Account2 { get; set; } = null!;


        public bool AllNonZeroRecordsGreaterThanMinimumThreshold =>
            (BillingStreetMatch != 0 || ShippingAddressMatch != 0 || NameMatch != 0 || OtherNameMatch != 0) &&
            BillingStreetMatch is 0 or >= MinimumPassValue &&
            ShippingAddressMatch is 0 or >= MinimumPassValue &&
            NameMatch is 0 or >= MinimumPassValue &&
            OtherNameMatch is 0 or >= MinimumPassValue;
    }
}
