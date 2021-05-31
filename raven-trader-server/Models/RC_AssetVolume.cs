using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace raven_trader_server.Models
{
    public class RC_AssetVolume
    {
        [Key]
        public int ID { get; set; }
        public int Block { get; set; }
        public string AssetName { get; set; }

        public double TransactionVolume { get; set; }
        public double SwapVolume { get; set; }
    }
}
