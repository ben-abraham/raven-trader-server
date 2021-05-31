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


        public SwapType Type { get; set; }

        public string InType { get; set; }
        public string OutType { get; set; }
        public double InQuantity { get; set; }
        public double OutQuantity { get; set; }
        public double UnitPrice
        {
            get
            {
                if (Type == SwapType.Buy)
                    return InQuantity / OutQuantity;
                else if (Type == SwapType.Sell)
                    return OutQuantity / InQuantity;
                else if (Type == SwapType.Trade)
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
