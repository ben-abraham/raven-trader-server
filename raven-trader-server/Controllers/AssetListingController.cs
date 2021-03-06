using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            bool valid = ListingEntry.TryParse(_rpc, _db, listing, out var result, out var error, true);


            //If we got a valid one, mark it as active, and save it to the db
            if (valid)
            {
                result.Active = true;
                //Listings can be updated if they use the same UTXO as a previously uploaded tx
                if(_db.Entry(result)?.State == EntityState.Detached)
                {
                    _db.Listings.Add(result);
                }
                _db.SaveChanges();
            }

            return new JsonResult(new ListingResult(valid, result, error));
        }

        [HttpPost]
        [Route("quickparse")]
        public JsonResult QuickParse([FromBody] ListingHex listing)
        {
            bool valid = ListingEntry.TryParse(_rpc, _db, listing, out var result, out var error, false);

            return new JsonResult(new ListingResult(valid, result, error));
        }
    }

    public class ListingResult
    {
        public ListingResult(bool valid, ListingEntry result, string error)
        {
            Valid = valid;
            Result = result;
            Error = error;
        }

        public bool Valid { get; set; }
        public ListingEntry Result { get; set; }
        public string Error { get; set; }
    }
}
