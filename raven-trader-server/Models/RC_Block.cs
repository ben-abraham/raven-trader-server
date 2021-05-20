using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class RC_Block
    {
        public int Number { get; set; }

        [Key]
        public string Hash { get; set; }

        public DateTime BlockTime { get; set; }
        public int Transactions { get; set; }
        public int Swaps { get; set; }
    }
}
