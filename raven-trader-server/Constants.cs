using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server
{
    public class Constants
    {
        public static int MAX_PAGE_SIZE = 100;

        public static string SINGLE_ANYONECANPAY = "[SINGLE|ANYONECANPAY]";

        public static char[] ASSET_SEPARATORS = new[] { '/', '#' };

        public const string VOUT_TYPE_NONSTANDARD = "nonstandard";
        public const string VOUT_TYPE_PUBKEY = "pubkey";
        public const string VOUT_TYPE_PUBKEYHASH = "pubkeyhash";
        public const string VOUT_TYPE_SCRIPTHASH = "scripthash";
        public const string VOUT_TYPE_MULTISIG = "multisig";
        public const string VOUT_TYPE_NULLDATA = "nulldata";
        public const string VOUT_TYPE_NULLASSETDATA = "nullassetdata";
        public const string VOUT_TYPE_WITNESS_V0_KEYHASH = "witness_v0_keyhash";
        public const string VOUT_TYPE_WITNESS_V0_SCRIPTHASH = "witness_v0_scripthash";

        public const string VOUT_TYPE_ISSUE_ASSET = "new_asset";
        public const string VOUT_TYPE_TRANSFER_ASSET = "transfer_asset";
        public const string VOUT_TYPE_REISSUE_ASSET = "reissue_asset";
    }
}
