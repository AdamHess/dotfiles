using AccountDeduplication.DAL.Models;

namespace AccountDeduplication.ProcessResults
{
    public class GroupMapping
    {
        public Account GroupLeader { get; set; }

        public IList<Account> GroupMembers { get; set; } = [];
    }
}
