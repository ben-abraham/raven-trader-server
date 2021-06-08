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


        public SwapType OrderType { get; set; }

        public string InType { get; set; }
        public string OutType { get; set; }
        public double InQuantity { get; set; }
        public double OutQuantity { get; set; }
        public double UnitPrice
        {
            get
            {
                if (OrderType == SwapType.Buy)
                    return InQuantity / OutQuantity;
                else if (OrderType == SwapType.Sell)
                    return OutQuantity / InQuantity;
                else if (OrderType == SwapType.Trade)
                    return InQuantity / OutQuantity;
                else
                    return 0;
            }
        }
    }

    public enum SwapType
    {
        Buy,
        Sell,
        Trade
    }
}
