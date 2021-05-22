using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using raven_trader_server.Models;

namespace raven_trader_server.Controllers
{
    [ApiController]
    [Route("api/assets")]
    public class AssetListingController : ControllerBase
    {
        private readonly ILogger<AssetListingController> _logger;
        private readonly RVN_RPC _rpc;
        private readonly RTDbContext _db;

        public AssetListingController(ILogger<AssetListingController> logger, RVN_RPC rpc, RTDbContext context)
        {
            _logger = logger;
            _rpc = rpc;
            _db = context;
        }

        [HttpGet]
        public IEnumerable<ListingEntry> Get()
        {
            return _db.Listings.ToList();
        }

        [HttpPost]
        [Route("list")]
        public JsonResult ListOrder([FromBody] ListingHex listing)
        {
            bool valid = false;
            ListingEntry entry = null;

            if(Regex.IsMatch(listing.Hex, @"^[0-9a-fA-F]*$"))
            {
                var parse_test = _rpc.DecodeRawTransaction(listing.Hex);

                if(parse_test != null)
                {
                    //Don't do anything special, if we sign and it says completed, that its properly validated to the owner of the UTXO
                    var sign_test = _rpc.SignRawTransaction(listing.Hex);

                    if(sign_test != null)
                    {
                        if (sign_test.Value<bool>("complete"))
                        {
                            valid = true;

                            dynamic parsed_tx = parse_test;

                            if (parsed_tx.vin[0]?.scriptSig?.asm?.ToString()?.Contains(Constants.SINGLE_ANYONECANPAY))
                            {
                                var utxo = $"{parsed_tx.vin[0].txid}|{parsed_tx.vin[0].vout}";
                                var type = parsed_tx.vout[0]?.scriptPubKey?.type == Constants.VOUT_TYPE_TRANSFER_ASSET ? SwapType.Buy : SwapType.Sell;

                                //var src_transaction = (dynamic)Utils.FullExternalTXDecode((string)parsed_tx.vin[0].txid);
                                var src_transaction = (dynamic)_rpc.GetRawTransaction((string)parsed_tx.vin[0].txid);
                                var src_vout = src_transaction.vout[(int)parsed_tx.vin[0].vout];

                                var existing = _db.Find<ListingEntry>(utxo);
                                if (existing != null)
                                    entry = existing;
                                else
                                {
                                    entry = new ListingEntry() { UTXO = utxo };
                                    _db.Listings.Add(entry);
                                }

                                entry.B64SignedPartial = Convert.ToBase64String(Utils.StringToByteArray(listing.Hex));
                                entry.OrderType = type;

                                switch (entry.OrderType)
                                {
                                    case SwapType.Buy:
                                        //For a buy order, the quantity is the amount being requested
                                        entry.Quantity = parsed_tx.vout[0].scriptPubKey.asset.amount;
                                        entry.AssetName = parsed_tx.vout[0].scriptPubKey.asset.name;
                                        entry.UnitPrice = src_vout.amount / entry.Quantity;
                                        break;
                                    case SwapType.Sell:
                                        entry.Quantity = src_vout.scriptPubKey.asset.amount;
                                        entry.AssetName = src_vout.scriptPubKey.asset.name;
                                        entry.UnitPrice = parsed_tx.vout[0].value / entry.Quantity;
                                        break;
                                }

                                _db.SaveChanges();
                            }
                        }
                    }
                }
            }

            return new JsonResult (new { valid = valid, result = entry });
        }

        [HttpPost]
        [Route("quickparse")]
        public JsonResult QuickParse([FromBody] ListingHex listing)
        {
            bool valid = false;

            if (Regex.IsMatch(listing.Hex, @"^[0-9a-fA-F]*$"))
            {
                var parse_test = _rpc.DecodeRawTransaction(listing.Hex);
                if (parse_test != null)
                {
                    dynamic parsed_tx = parse_test;

                    if (parsed_tx.vin[0]?.scriptSig?.asm?.ToString()?.Contains(Constants.SINGLE_ANYONECANPAY))
                    {
                        var utxo = $"{parsed_tx.vin[0].txid}|{parsed_tx.vin[0].vout}";
                        var type = parsed_tx.vout[0]?.scriptPubKey?.type == Constants.VOUT_TYPE_TRANSFER_ASSET ? SwapType.Buy : SwapType.Sell;

                        //var src_transaction = (dynamic)Utils.FullExternalTXDecode((string)parsed_tx.vin[0].txid);
                        var src_transaction = (dynamic)_rpc.GetRawTransaction((string)parsed_tx.vin[0].txid);
                        var src_vout = src_transaction.vout[(int)parsed_tx.vin[0].vout];

                        ListingEntry entry = new ListingEntry();

                        entry.B64SignedPartial = Convert.ToBase64String(Utils.StringToByteArray(listing.Hex));
                        entry.OrderType = type;
                        entry.UTXO = utxo;

                        switch (entry.OrderType)
                        {
                            case SwapType.Buy:
                                //For a buy order, the quantity is the amount being requested
                                entry.Quantity = (double)parsed_tx.vout[0].scriptPubKey.asset.amount;
                                entry.AssetName = parsed_tx.vout[0].scriptPubKey.asset.name.ToString();
                                entry.UnitPrice = (entry.Quantity == 0) ? 0 : src_vout.value / entry.Quantity;
                                break;
                            case SwapType.Sell:
                                entry.Quantity = (double)src_vout.scriptPubKey.asset.amount;
                                entry.AssetName = src_vout.scriptPubKey.asset.name.ToString();
                                entry.UnitPrice = (entry.Quantity == 0) ? 0 : ((double)parsed_tx.vout[0].value) / entry.Quantity;
                                break;
                        }

                        return new JsonResult(new
                        {
                            valid = true,
                            result = entry
                        });
                    }
                }
            }

            return new JsonResult(new { valid = valid, result = (object)null });
        }
    }

    public class ListingHex
    {
        public string Hex { get; set; }
        public string Memo { get; set; }
    }
}
