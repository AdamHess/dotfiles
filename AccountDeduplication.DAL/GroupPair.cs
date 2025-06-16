using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountDeduplication.DAL
{
    public enum Phase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
        Phase4 = 4
    }
    
    public class GroupPair
    {
        public string AccountId { get; set; }
        public Phase Phase { get; set; }
        public string GroupAccountId { get; set; }
        public double MatchPercentage { get; set; }
        
        
        public Account Account { get; set; }
        public Account GroupAccount { get; set; }
    }
}
