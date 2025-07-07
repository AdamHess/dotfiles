using AccountDeduplication.DAL.Models;

namespace AccountDeduplication.ProcessResults
{
    public class GroupMapping
    {
        public Account GroupLeader { get; set; }

        public List<Account> GroupMembers { get; set; } = [];
    }
}
