using AccountDeduplication.DAL.Models;

namespace AccountDeduplication.CsvModels;

public class DataLoadModelPerson : DataloadBaseModel
{
    public DataLoadModelPerson()
    {

    }
    public override void Initialize(Account account, Account groupLeader)
    {
        base.Initialize(account, groupLeader);

        FirstName = groupLeader.FirstName != account.FirstName ? groupLeader.FirstName : null;
        LastName = groupLeader.LastName != account.LastName ? groupLeader.LastName : null;

    }


    public string FirstName { get; set; }
    public string LastName { get; set; }
    public override bool AnyNonNullVals()
    {
        return BillingAddress != null || ShippingAddress != null || OtherOrgName != null || LastName != null || FirstName != null;
    }

}

public class DataLoadModelBusiness : DataloadBaseModel
{
    public DataLoadModelBusiness()
    {

    }
    public override void Initialize(Account account, Account groupLeader)
    {
        base.Initialize(account, groupLeader);
        Name = groupLeader.IsPersonAccount && groupLeader.Name != account.Name ? groupLeader.Name : null;
    }

    public string Name { get; set; }

    public override bool AnyNonNullVals()
    {
        return BillingAddress != null || ShippingAddress != null || OtherOrgName != null || Name != null;
    }

}