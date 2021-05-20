using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class RC_Swap
    {
        [Key]
        public string TXID { get; set; }
        public int Block { get; set; }


        public string AssetName { get; set; }
        public SwapType Type { get; set; }
        public float Quantity { get; set; }
        public float UnitPrice { get; set; }
    }

    public enum SwapType
    {
        Buy,
        Sell,
        Exchange
    }
}
