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
        public string AssetName { get; set; }
        public double Quantity { get; set; }
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
            }

            var utxo = $"{parsed_tx.vin[0].txid}-{parsed_tx.vin[0].vout}";
            var type = parsed_tx.vout[0]?.scriptPubKey?.type == Constants.VOUT_TYPE_TRANSFER_ASSET ? SwapType.Buy : SwapType.Sell;

            //Must be fully txindexed to be able to use GetRawTransaction arbitrarily
            //var src_transaction = (dynamic)Utils.FullExternalTXDecode((string)parsed_tx.vin[0].txid);
            var src_transaction = (dynamic)rpc.GetRawTransaction((string)parsed_tx.vin[0].txid);
            var src_vout = src_transaction.vout[(int)parsed_tx.vin[0].vout];

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
            Value.OrderType = type;

            switch (Value.OrderType)
            {
                case SwapType.Buy:
                    //For a buy order, the quantity is the amount being requested
                    Value.Quantity = parsed_tx.vout[0].scriptPubKey.asset.amount;
                    Value.AssetName = parsed_tx.vout[0].scriptPubKey.asset.name;
                    Value.UnitPrice = src_vout.amount / Value.Quantity;
                    break;
                case SwapType.Sell:
                    Value.Quantity = src_vout.scriptPubKey.asset.amount;
                    Value.AssetName = src_vout.scriptPubKey.asset.name;
                    Value.UnitPrice = parsed_tx.vout[0].value / Value.Quantity;
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
