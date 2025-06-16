using AccountDeduplication.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountDeduplication.ProcessResults
{
    public class GroupMapping
    {
        public Account GroupLeader {get; set;}
        
        public IList<Account> GroupMembers { get; set; }
    }
}
