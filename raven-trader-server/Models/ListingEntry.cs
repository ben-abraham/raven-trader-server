using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class ListingEntry
    {
        [Key]
        public string UTXO { get; set; }
        public string B64SignedPartial { get; set; }
        public SwapType OrderType { get; set; }
        public string AssetName { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
    }
}
