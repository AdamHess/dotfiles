using AccountDeduplication.DAL.Models;

namespace AccountDeduplication.CsvModels
{
    public abstract class DataloadBaseModel
    {

        public virtual void Initialize(Account account, Account groupLeader)
        {
            Id = account.Id;
            OtherOrgName =
                groupLeader.OtherOrgName != account.OtherOrgName
                    ? groupLeader.OtherOrgName
                    : null;
            BillingAddress = groupLeader.BillingStreetRaw != account.BillingStreetRaw
                ? groupLeader.BillingStreetRaw
                : null;
            ShippingAddress =
                groupLeader.ShippingStreetRaw != account.ShippingStreetRaw
                    ? groupLeader.ShippingStreetRaw
                    : null;
        }

        public string Id { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }
        public string OtherOrgName { get; set; }
        public abstract bool AnyNonNullVals();
    }
}


