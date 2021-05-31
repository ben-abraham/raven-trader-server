using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class ListingEntry
    {
        [Key]
        public string UTXO { get; set; }
        public string Memo { get; set; }
        public string B64SignedPartial { get; set; }
        public SwapType OrderType { get; set; }
        
        public string InType { get; set; }
        public string OutType { get; set; }
        public double InQuantity { get; set; }
        public double OutQuantity { get; set; }

        public double UnitPrice { get; set; }

        public DateTime SubmitTime { get; set; }
        public bool Active { get; set; }
        public int ExecutedBlock { get; set; }
        public string ExecutedTXID { get; set; }


        public static bool TryParse(RVN_RPC rpc, RTDbContext db, ListingHex listing, out ListingEntry Value, out string Error, bool ValidateSignature = true)
        {
            Value = null;
            Error = null;

            if (!Regex.IsMatch(listing.Hex, @"^[0-9a-fA-F]*$"))
            {
                Error = "Invalid input format.";
                return false;
            }

            JObject decodedTX = null;
            try
            {
                decodedTX = rpc.DecodeRawTransaction(listing.Hex);
            }
            catch { }

            if (decodedTX == null)
            {
                Error = "Unable to decode transaction.";
                return false;
            }

            if (ValidateSignature)
            {
                //Don't do anything special, if we sign and it says completed, that its properly validated to the owner of the UTXO
                var sign_test = rpc.SignRawTransaction(listing.Hex);

                if ((sign_test?.Value<bool>("complete") ?? false) == false)
                {
                    Error = "Signature is invalid";
                    return false;
                }
            }

            dynamic parsed_tx = decodedTX;

            if (!parsed_tx.vin[0]?.scriptSig?.asm?.ToString()?.Contains(Constants.SINGLE_ANYONECANPAY))
            {
                Error = "Transaction is not signed with SINGLE|ANYONECANPAY. Not a valid swap.";
                return false;
            }

            var utxo = $"{parsed_tx.vin[0].txid}-{parsed_tx.vin[0].vout}";

            //Must be fully txindexed to be able to use GetRawTransaction arbitrarily
            //var src_transaction = (dynamic)Utils.FullExternalTXDecode((string)parsed_tx.vin[0].txid);
            var src_transaction = (dynamic)rpc.GetRawTransaction((string)parsed_tx.vin[0].txid);
            var src_vout = src_transaction.vout[(int)parsed_tx.vin[0].vout];

            var in_type = src_vout?.scriptPubKey?.type;
            var out_Type = parsed_tx.vout[0]?.scriptPubKey?.type;

            SwapType? type = null;

            if (in_type == Constants.VOUT_TYPE_TRANSFER_ASSET && out_Type == Constants.VOUT_TYPE_TRANSFER_ASSET)
                type = SwapType.Trade;
            else if (out_Type == Constants.VOUT_TYPE_TRANSFER_ASSET)
                type = SwapType.Buy;
            else if (in_type == Constants.VOUT_TYPE_TRANSFER_ASSET)
                type = SwapType.Sell;

            if(type == null)
            {
                Error = "Swap does not contain a recognized Input/Output transaction type combination";
                return false;
            }

            var existing = db.Listings.SingleOrDefault(l => l.UTXO == utxo);

            if (existing == null)
            {
                Value = new ListingEntry()
                {
                    UTXO = utxo
                };
            }
            else
            {
                Value = existing;
            }

            Value.SubmitTime = DateTime.UtcNow;
            Value.B64SignedPartial = Convert.ToBase64String(Utils.StringToByteArray(listing.Hex));
            Value.OrderType = type.Value;

            switch (Value.OrderType)
            {
                case SwapType.Buy:
                    //For a buy order, the quantity is the amount being requested
                    Value.InType = "rvn";
                    Value.InQuantity = src_vout.value;
                    Value.OutType = parsed_tx.vout[0].scriptPubKey.asset.name;
                    Value.OutQuantity = parsed_tx.vout[0].scriptPubKey.asset.amount;

                    Value.UnitPrice = Value.InQuantity / Value.OutQuantity;
                    break;
                case SwapType.Sell:
                    Value.InType = src_vout.scriptPubKey.asset.name;
                    Value.InQuantity = src_vout.scriptPubKey.asset.amount;
                    Value.OutType = "rvn";
                    Value.OutQuantity = parsed_tx.vout[0].value;

                    Value.UnitPrice = Value.OutQuantity / Value.InQuantity;
                    break;
                case SwapType.Trade:
                    Value.InType = src_vout.scriptPubKey.asset.name;
                    Value.InQuantity = src_vout.scriptPubKey.asset.amount;
                    Value.OutType = parsed_tx.vout[0].scriptPubKey.asset.name;
                    Value.OutQuantity = parsed_tx.vout[0].scriptPubKey.asset.amount;

                    Value.UnitPrice = Value.InQuantity / Value.OutQuantity;
                    break;
            }
            return true;
        }

    }

    public class ListingHex
    {
        public string Hex { get; set; }
        public string Memo { get; set; }
    }

}
