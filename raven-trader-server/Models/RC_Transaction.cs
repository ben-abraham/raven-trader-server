using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class RC_Transaction
    {
        [Key]
        public string Hash { get; set; }

        public static bool IsSwapTransaction(RVN_RPC RPC, dynamic DecodedTransaction, int Block, out RC_Swap Swap)
        {
            Swap = null;

            //if (!(DecodedTransaction.vin.Count >= 1)) return false;
            var vin_asm = DecodedTransaction.vin?[0]?.scriptSig?.asm?.ToString();

            if (vin_asm == null) return false;
            if (!vin_asm.Contains(Constants.SINGLE_ANYONECANPAY)) return false;

            //Ins/Outs of the person who setup the swap
            var swap_setup_vin = DecodedTransaction.vin[0];
            var swap_setup_vout = DecodedTransaction.vout[0];

            //Ins/Outs of the person who executed the swap
            var swap_result_vin = new List<dynamic>();// DecodedTransaction.vin.Skip(1).ToList();
            var swap_result_vout = new List<dynamic>();// DecodedTransaction.vout.Skip(1).ToList();

            for (int vin_idx = 1; vin_idx < DecodedTransaction.vin.Count; vin_idx++)
            {
                var tx_vin = DecodedTransaction.vin[vin_idx];

                if (tx_vin.scriptSig?.asm?.ToString()?.Contains(Constants.SINGLE_ANYONECANPAY))
                {
                    //If we see [SINGLE|ANYONECANPAY] in anything other than the first vin, this is a complex swap. skip for now.
                    return false;
                }

                swap_result_vin.Add(tx_vin);
            }
            for (int vout_idx = 1; vout_idx < DecodedTransaction.vout.Count; vout_idx++) swap_result_vout.Add(DecodedTransaction.vout[vout_idx]);

            dynamic swap_setup_src = RPC.GetRawTransaction(swap_setup_vin.txid.ToString());
            dynamic swap_setup_src_vout = swap_setup_src.vout[(int)swap_setup_vin.vout];
            SwapType swapType;
            float quantity, unitPrice;
            string assetName;

            if(swap_setup_vout?.scriptPubKey?.type == Constants.VOUT_TYPE_TRANSFER_ASSET)//Setup party wanted assets
            {
                
                if(swap_setup_src_vout?.scriptPubKey?.type == Constants.VOUT_TYPE_TRANSFER_ASSET)//Setup party provided assets
                {
                    swapType = SwapType.Exchange;

                    return false;
                }
                else//pubkeyhash (or scripthash?), Setup party provided RVN. So it's a buy
                {
                    swapType = SwapType.Buy;

                    assetName = swap_setup_vout.scriptPubKey.asset.name;
                    quantity = swap_setup_vout.scriptPubKey.asset.amount;
                    unitPrice = swap_setup_src_vout.value / quantity;
                }
            }
            else//Setup party wanted RVN
            {
                if (swap_setup_src_vout?.scriptPubKey?.type == Constants.VOUT_TYPE_TRANSFER_ASSET) //Setup party provided assets, so it's a sell
                {
                    swapType = SwapType.Sell;

                    assetName = swap_setup_src_vout.scriptPubKey.asset.name;
                    quantity = swap_setup_src_vout.scriptPubKey.asset.amount;
                    unitPrice = swap_setup_vout.value / quantity;
                }
                else
                {
                    //wTF are they doing? RVN-RVN swap....?

                    return false;
                }
            }

            Swap = new RC_Swap()
            {
                TXID = DecodedTransaction.txid.ToString(),
                Block = Block,
                Type = swapType,
                AssetName = assetName,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
            return true;
        }
    }
}
