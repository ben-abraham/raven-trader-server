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
        public SiteDataController(RTDbContext db)
        {
            _db = db;
        }

        public RTDbContext _db { get; }

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
        public JsonResult GetGrouped(string assetName, string swapType, bool groupListing = false, int pageSize = 100, int offset = 0)
        {
            if (pageSize > Constants.MAX_PAGE_SIZE) pageSize = Constants.MAX_PAGE_SIZE;

            //A bit expensive to look through all orders like this, but for now its okay :shrug:
            var swapQuery = _db.Listings.AsQueryable().Where(l => l.Active);
            if (!string.IsNullOrEmpty(assetName))
                swapQuery = swapQuery.Where(s => s.InType.Contains(assetName) || s.OutType.Contains(assetName));
            
            var assetList = swapQuery.Select(l => l.OutType).Union(swapQuery.Select(l => l.InType))
                .Where(t => t != "rvn")
                .OrderBy(t => t)
                .ToList();

            int totalCount = assetList.Count();
            var pageAssets = assetList.Skip(offset).Take(pageSize);

            //Note: This is not SQL based and is therefore expesive AF
            var assetResult = pageAssets.Select(pa =>
            {
                var buyOrders = _db.Listings
                        .Where(l => l.OrderType == SwapType.Sell && l.InType.Contains(pa))
                        .OrderBy(l => l.UnitPrice);
                var sellOrders = _db.Listings
                        .Where(l => l.OrderType == SwapType.Buy && l.OutType.Contains(pa))
                        .OrderByDescending(l => l.UnitPrice);
                var tradeOrders = _db.Listings
                        .Where(l => l.OrderType == SwapType.Trade && (l.InType.Contains(pa) || l.OutType.Contains(pa)))
                        .OrderByDescending(l => l.UnitPrice);

                //Buy/Sell perspective is reversed as always here
                return new
                {
                    Asset = pa,
                    BuyOrders = buyOrders.Count(),
                    BuyQuantity = buyOrders.Sum(o => o.OutQuantity),
                    MinBuy = buyOrders.FirstOrDefault(),
                    SellOrders = sellOrders.Count(),
                    SellQuantity = sellOrders.Sum(o => o.InQuantity),
                    MaxSell = sellOrders.FirstOrDefault(),
                    TradeOrders = tradeOrders.Count(),
                    TradeQuantity = tradeOrders.Sum(o => o.InType == pa ? o.InQuantity : o.OutQuantity),
                    Trades = tradeOrders.FirstOrDefault()
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
        public JsonResult GetHistory(string assetName, string swapType, int pageSize = 100, int offset = 0)
        {
            if (pageSize > Constants.MAX_PAGE_SIZE) pageSize = Constants.MAX_PAGE_SIZE;

            var swapQuery = _db.Swaps.AsQueryable();

            if (!string.IsNullOrEmpty(assetName))
                swapQuery = swapQuery.Where(s => s.InType.Contains(assetName) || s.OutType.Contains(assetName));
            if (!string.IsNullOrEmpty(swapType) && Enum.TryParse<SwapType>(swapType, out var parsedType))
                swapQuery = swapQuery.Where(s => s.Type == parsedType);

            var finalQuery = swapQuery.OrderByDescending(s => s.Block).Skip(offset).Take(pageSize);
            var totalCount = swapQuery.Count();

            return new JsonResult(new
            {
                offset = offset,
                totalCount = totalCount,
                swaps = finalQuery.ToArray()
            });
        }
    }
}
