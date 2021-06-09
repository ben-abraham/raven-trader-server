using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using raven_trader_server.Models;

namespace raven_trader_server.Controllers
{
    [Route("api/sitedata")]
    [ApiController]
    public class SiteDataController : ControllerBase
    {
        public SiteDataController(RTDbContext db, RVN_RPC rpc)
        {
            _db = db;
            _rpc = rpc;
        }

        public RTDbContext _db { get; }
        public RVN_RPC _rpc { get; }

        [HttpGet]
        public JsonResult Get()
        {
            var head = _db.Blocks.OrderByDescending(b => b.Number).FirstOrDefault();
            var swaps = _db.Swaps.OrderByDescending(s => s.Block).Take(50);

            //If the site is really out of date.
            if (head != null)
            {
                var pastCutoff = DateTime.Now - TimeSpan.FromHours(24);

                var dayBlock = _db.Blocks.Where(b => b.BlockTime >= pastCutoff).OrderBy(b => b.Number).FirstOrDefault();

                //If no block, we are WAY behind....
                if (dayBlock != null)
                {
                    var dayVolume = _db.AssetVolume.Where(av => av.Block >= dayBlock.Number)
                        .GroupBy(av => av.AssetName)
                        .Select(avg => new Models.RC_AssetVolume()
                        {
                            Block = dayBlock.Number,//dummy
                            AssetName = avg.Key,
                            SwapVolume = avg.Sum(av => av.SwapVolume),
                            TransactionVolume = avg.Sum(av => av.TransactionVolume)
                        })
                        .ToDictionary(av => av.AssetName, av => new { total = av.TransactionVolume, swap = av.SwapVolume });

                    return new JsonResult(new
                    {
                        block = head.Number,
                        hash = head.Hash,
                        recent_swaps = swaps,
                        asset_volume = dayVolume
                    });
                }
            }

            return new JsonResult(new
            {
                block = head?.Number,
                hash = head?.Hash,
                recent_swaps = swaps,
                asset_volume = new { }
            });
        }

        [HttpGet]
        [Route("groupedlistings")]
        public JsonResult GetGroupedListings(string assetName, string swapType, int pageSize = 100, int offset = 0)
        {
            if (pageSize > Constants.MAX_PAGE_SIZE) pageSize = Constants.MAX_PAGE_SIZE;

            //A bit expensive to look through all orders like this, but for now its okay :shrug:
            var listQuery = _db.Listings.AsQueryable().Where(l => l.Active);
            if (!string.IsNullOrEmpty(assetName))
                listQuery = listQuery.Where(s => s.InType.Contains(assetName) || s.OutType.Contains(assetName));
            
            var assetList = listQuery.Select(l => l.OutType).Union(listQuery.Select(l => l.InType))
                .Where(t => t != "rvn")
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            int totalCount = assetList.Count();
            var pageAssets = assetList.Skip(offset).Take(pageSize);

            //Note: This is not SQL optimized and is therefore expesive AF
            var assetResult = pageAssets.Select(pa =>
            {
                var assetOrders = listQuery
                    .Where(l => l.InType == pa || l.OutType == pa)
                    .OrderBy(l => l.UnitPrice).ToList();

                //Buy/Sell perspective is reversed as always here
                //Reverse sells to make the orders meet in the middle
                var buyOrders = assetOrders.Where(l => l.OrderType == SwapType.Sell)
                    .OrderBy(l => l.UnitPrice).ThenBy(l => l.InQuantity).ToList();
                var sellOrders = assetOrders.Where(l => l.OrderType == SwapType.Buy)
                    .OrderByDescending(l => l.UnitPrice).ThenBy(l => l.OutQuantity).ToList();
                var tradeOrders = assetOrders.Where(l => l.OrderType == SwapType.Trade)
                    .OrderBy(l => l.UnitPrice).ThenBy(l => l.InType == pa ? l.InQuantity : l.OutQuantity).ToList();

                return new
                {
                    Asset = pa,
                    BuyOrders = buyOrders.Count(),
                    BuyQuantity = buyOrders.Sum(o => o.InQuantity),
                    MinBuy = buyOrders.FirstOrDefault(),
                    SellOrders = sellOrders.Count(),
                    SellQuantity = sellOrders.Sum(o => o.OutQuantity),
                    MaxSell = sellOrders.FirstOrDefault(),
                    TradeOrders = tradeOrders.Count(),
                    TradeQuantity = tradeOrders.Sum(o => o.InType == pa ? o.InQuantity : o.OutQuantity)
                };
            });

            return new JsonResult(new
            {
                offset = offset,
                totalCount = totalCount,
                assets = assetResult
            });
        }

        [HttpGet]
        [Route("listings")]
        public JsonResult GetListings(string assetName, string swapType, bool groupListing=false, int pageSize = 100, int offset = 0)
        {
            if (pageSize > Constants.MAX_PAGE_SIZE) pageSize = Constants.MAX_PAGE_SIZE;

            var swapQuery = _db.Listings.AsQueryable().Where(l => l.Active);
            if (!string.IsNullOrEmpty(assetName))
                swapQuery = swapQuery.Where(s => s.InType.Contains(assetName) || s.OutType.Contains(assetName));
            if (!string.IsNullOrEmpty(swapType) && Enum.TryParse<SwapType>(swapType, out var parsedType))
                swapQuery = swapQuery.Where(s => s.OrderType == parsedType);

            var finalQuery = swapQuery.OrderByDescending(l => l.SubmitTime).Skip(offset).Take(pageSize);
            var totalCount = swapQuery.Count();


            return new JsonResult(new {
                offset = offset,
                totalCount = totalCount,
                swaps = finalQuery.ToArray()
            });
        }

        [HttpGet]
        [Route("swaphistory")]
        public JsonResult GetHistory(string assetName, string swapType, bool searchAny = false, int pageSize = 100, int offset = 0)
        {
            if (pageSize > Constants.MAX_PAGE_SIZE) pageSize = Constants.MAX_PAGE_SIZE;

            var swapQuery = _db.Swaps.AsQueryable();

            if (!string.IsNullOrEmpty(assetName))
                if(searchAny)
                    swapQuery = swapQuery.Where(s => s.InType.Contains(assetName) || s.OutType.Contains(assetName));
                else
                    swapQuery = swapQuery.Where(s => s.InType == assetName || s.OutType == assetName);
            if (!string.IsNullOrEmpty(swapType) && Enum.TryParse<SwapType>(swapType, out var parsedType))
                swapQuery = swapQuery.Where(s => s.OrderType == parsedType);

            var finalQuery = swapQuery.OrderByDescending(s => s.Block).Skip(offset).Take(pageSize);
            var totalCount = swapQuery.Count();

            return new JsonResult(new
            {
                offset = offset,
                totalCount = totalCount,
                swaps = finalQuery.ToArray()
            });
        }

        [HttpGet]
        [Route("asset")]
        public JsonResult GetAssetDetails(string assetName)
        {
            //TODO: NO RPC CALLS DURING WEB REQUESTS UNLESS NEEDED
            //TODO: Properly Validate asset name input
            if (assetName == null || assetName.Length > 31 || assetName.Length < 4)
                return new JsonResult(null);
            
            var asset_data = _rpc.GetAssetData(assetName);

            if (asset_data == null)
                return new JsonResult(null);
            
            var child_assets = _rpc.ListAssets($"{assetName}/*").Union(_rpc.ListAssets($"{assetName}#*"));

            var parent_asset = assetName.Any(c => Constants.ASSET_SEPARATORS.Contains(c)) ?
                assetName.Substring(0, Constants.ASSET_SEPARATORS.Max(s => assetName.LastIndexOf(s))) : null;

            var assetOrders = _db.Listings.AsQueryable()
                .Where(l => l.Active)
                .Where(l => l.InType == assetName || l.OutType == assetName)
                .OrderBy(l => l.UnitPrice);

            var buyOrders = assetOrders.Where(l => l.OrderType == SwapType.Sell).Reverse().Take(100).ToList();
            var sellOrders = assetOrders.Where(l => l.OrderType == SwapType.Buy).Take(100).ToList();
            var tradeOrders = assetOrders.Where(l => l.OrderType == SwapType.Trade).Take(100).ToList();

            return new JsonResult(new
            {
                Asset = assetName,
                Parent = parent_asset,
                Children = child_assets,
                Units = asset_data.Value<int>("units"),
                Denomination = 1 / Math.Pow(10, asset_data.Value<int>("units")),
                Amount = asset_data.Value<float>("amount"),
                ipfs = asset_data.ContainsKey("ipfs_hash") ? asset_data.Value<string>("ipfs_hash") : null,
                Reissuable = asset_data.Value<int>("reissuable") == 1,
                BuyOrders = buyOrders.Count(),
                BuyQuantity = buyOrders.Sum(o => o.InQuantity),
                Buys = buyOrders,
                SellOrders = sellOrders.Count(),
                SellQuantity = sellOrders.Sum(o => o.OutQuantity),
                Sells = sellOrders,
                TradeOrders = tradeOrders.Count(),
                TradeQuantity = tradeOrders.Sum(o => o.InType == assetName ? o.InQuantity : o.OutQuantity),
                Trades = tradeOrders
            });
        }
    }
}
