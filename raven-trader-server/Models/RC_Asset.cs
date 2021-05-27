using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class RC_Asset
    {
        [Key]
        public string Name { get; set; }

        public AssetType Type { get; set; }

        public int Block { get; set; }

        public double Amount { get; set; }
        public int Units { get; set; }
        public bool Reissuable { get; set; }
        //public string BlockHash { get; set; }
        public string IPFS_Hash { get; set; }
    }

    public enum AssetType
    {
        Asset,
        Unique,
        Restricted,
        Qualifier,
    }
}
