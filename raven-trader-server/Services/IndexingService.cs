using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using raven_trader_server.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace raven_trader_server.Services
{
    public class IndexingService : IHostedService, IDisposable
    {
        private readonly ILogger<IndexingService> _logger;
        private readonly IConfiguration _config;
        private readonly RTDbContext _db;
        private readonly RVN_RPC _rpc;

        private readonly IServiceScope _indexScope;

        private Timer _timer;

        private RC_Block lastBlock;

        int MaxPerTimer = 1000;

        public IndexingService(ILogger<IndexingService> logger, RVN_RPC rpc, IConfiguration config,
            IServiceScopeFactory scopeFactory, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _config = config;
            _rpc = rpc;
            _indexScope = scopeFactory.CreateScope();
            _db = _indexScope.ServiceProvider.GetRequiredService<RTDbContext>();

            lastBlock = _db.Blocks.OrderByDescending(b => b.Number).FirstOrDefault();

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        private void OnShutdown()
        {
            //this code is called when the application stops
            _logger.LogInformation("Got Shutdown");
            StopAsync(default);
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            lock (_timer) //Lock the timer to prevent any issues
            {
                BlockIndexer();
            }
        }


        private void BlockIndexer()
        {
            //_logger.LogInformation($"Indexer cycle");
            List<RC_Block> blockAdds = new List<RC_Block>();
            List<RC_Swap> swapAdds = new List<RC_Swap>();
            List<RC_Asset> assetAdds = new List<RC_Asset>();
            List<RC_AssetVolume> volumeAdds = new List<RC_AssetVolume>();
            List<ListingEntry> completedListings = new List<ListingEntry>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int numBlocks;
            int nextBlockNumber;
            if(lastBlock == null)
            {
                _logger.LogWarning("Indexer starting fresh.");
                if(!int.TryParse(_config["IndexStart"], out nextBlockNumber))
                {
                    _logger.LogWarning("No configured 'IndexStart', starting from 0");
                    nextBlockNumber = 0;
                }
            }
            else
            {
                nextBlockNumber = lastBlock.Number + 1;
            }

            var totalBlocks = _rpc.GetNumBlocks();
            bool caughtUp = (nextBlockNumber + 10) >= totalBlocks;
            //Anything within 10 blocks of head is considered "done" for logging

            for (numBlocks = 0; numBlocks < MaxPerTimer; numBlocks++)
            {
                //If we are all caught up, then don't process any more
                if (nextBlockNumber > totalBlocks) break;

                var blockIndexResults = IndexBlock(nextBlockNumber);

                blockAdds.Add(blockIndexResults.block);
                swapAdds.AddRange(blockIndexResults.swaps);
                volumeAdds.AddRange(blockIndexResults.volume);
                assetAdds.AddRange(blockIndexResults.assets);
                completedListings.AddRange(blockIndexResults.listings);

                lastBlock = blockIndexResults.block;
                nextBlockNumber = lastBlock.Number + 1;
            }

            sw.Stop();

            if (blockAdds.Count > 0)
            {
                _db.Blocks.AddRange(blockAdds);
                _db.Swaps.AddRange(swapAdds);
                _db.AssetVolume.AddRange(volumeAdds);
                _db.Assets.AddRange(assetAdds.GroupBy(a => a.Name).Select(a => a.OrderByDescending(sort => sort.Block).FirstOrDefault()));
                _db.SaveChanges();

                string extra = null;
                //Don't log time stats for every block
                if (!caughtUp && sw.Elapsed.TotalSeconds != 0.00)
                {
                    extra = $"[ {numBlocks / sw.Elapsed.TotalSeconds} BPS ]";
                }

                int numListingsRemoved = completedListings.Count;

                _logger.LogInformation($"Saved. Current HEAD - {lastBlock?.Number ?? 0}/{totalBlocks}. Total Volume - {volumeAdds.Sum(av => av.TransactionVolume)} Assets {extra} Listings Completed: {numListingsRemoved}");
            }
        }

        private (
            RC_Block block, 
            List<RC_AssetVolume> volume, 
            List<RC_Swap> swaps, 
            List<RC_Asset> assets,
            List<ListingEntry> listings
            ) 
            IndexBlock(int blockNumber)
        {
            var blockHash = _rpc.GetBlockHash(blockNumber);
            dynamic blockDetails = _rpc.GetBlock(blockHash, 2).ToObject<dynamic>();
            
            List<RC_Swap> foundSwaps = new List<RC_Swap>();
            List<RC_Asset> foundAssets = new List<RC_Asset>();
            Dictionary<string, (string, int)> usedUTXOs = new Dictionary<string, (string, int)>();

            Dictionary<string, RC_AssetVolume> assetVolumes = new Dictionary<string, RC_AssetVolume>();

            Action<string, double, bool> addAssetVolume = (asset_name, quantity, as_swap) =>
            {
                if (quantity == 0) return;

                RC_AssetVolume vol = assetVolumes.ContainsKey(asset_name) ? assetVolumes[asset_name] : new RC_AssetVolume()
                {
                    AssetName = asset_name,
                    Block = blockNumber,
                };
                if (as_swap)
                    vol.SwapVolume += quantity;
                else
                    vol.TransactionVolume += quantity;
                assetVolumes[asset_name] = vol;
            };

            foreach (var block_tx in blockDetails.tx)
            {
                if (RC_Transaction.IsSwapTransaction(_rpc, block_tx, blockNumber, out RC_Swap swap))
                {
                    foundSwaps.Add(swap);

                    addAssetVolume(swap.InType, swap.InQuantity, true);
                    addAssetVolume(swap.InType, swap.InQuantity, true);
                }

                foreach(var vin in block_tx.vin)
                {
                    if (!string.IsNullOrEmpty(vin.txid?.ToString()))
                    {
                        var utxo = $"{vin.txid}-{vin.vout}";
                        usedUTXOs.Add(utxo, (blockDetails.hash.ToString(), (int)blockDetails.height));
                    }
                }

                //Swaps will also be picked up by this, no need to add them to the count after
                foreach (var vout in block_tx.vout)
                {
                    string outType = (string)vout?.scriptPubKey?.type;
                    switch (outType)
                    {
                        case Constants.VOUT_TYPE_TRANSFER_ASSET:
                            var asset_name = vout.scriptPubKey.asset.name.ToString();
                            var quantity = (float)vout.scriptPubKey.asset.amount;

                            addAssetVolume(asset_name, quantity, false);
                            break;
                        case Constants.VOUT_TYPE_ISSUE_ASSET:
                        case Constants.VOUT_TYPE_REISSUE_ASSET:
                            //Disabled for now.
                            //foundAssets.Add(IndexAsset(blockNumber, vout, AssetType.Asset));
                            break;
                        case Constants.VOUT_TYPE_NULLASSETDATA:
                            //TODO: Restricted asset data?
                            break;
                        case Constants.VOUT_TYPE_NONSTANDARD:
                        case Constants.VOUT_TYPE_PUBKEY:
                        case Constants.VOUT_TYPE_PUBKEYHASH:
                        case Constants.VOUT_TYPE_SCRIPTHASH:
                        case Constants.VOUT_TYPE_MULTISIG:
                        case Constants.VOUT_TYPE_NULLDATA:
                        case Constants.VOUT_TYPE_WITNESS_V0_KEYHASH:
                        case Constants.VOUT_TYPE_WITNESS_V0_SCRIPTHASH:
                            break;
                        default:
                            break;
                    }
                }
            }

            DateTime blockTime = DateTimeOffset.FromUnixTimeSeconds((long)blockDetails.Value<long>("time")).DateTime.ToUniversalTime();

            var newBlock = new RC_Block()
            {
                BlockTime = blockTime,
                Hash = blockHash,
                Number = blockNumber,
                Swaps = foundSwaps.Count,
                Transactions = blockDetails.tx.Count
            };

            var completedListings = ScanCompletedListings(usedUTXOs).ToList();

            //_logger.LogInformation($"Indexed block {newBlock.Number} - {newBlock.Hash}");

            if (foundSwaps.Count > 0)
            {
                _logger.LogInformation($"Found {foundSwaps.Count} swaps on block {blockNumber} - {blockHash}");
            }

            return (newBlock, assetVolumes.Values.ToList(), foundSwaps, foundAssets, completedListings);
        }

        public RC_Asset IndexAsset(int block, dynamic transactionPayload, AssetType type)
        {
            string name = (string)transactionPayload.scriptPubKey.asset.name;

            var existingAsset = _db.Assets.SingleOrDefault(a => a.Name == name);
            
            if(existingAsset == null)
            {
                existingAsset = new RC_Asset()
                {
                    Name = name
                };
            }

            existingAsset.Block = block;
            existingAsset.Amount = (double)transactionPayload.scriptPubKey.asset.amount;
            existingAsset.Units = (int)(transactionPayload.scriptPubKey.asset.units ?? 1);
            existingAsset.Reissuable = ((int)(transactionPayload.scriptPubKey.asset.reissuable ?? 0)) == 1;
            existingAsset.IPFS_Hash = (string)transactionPayload.scriptPubKey.asset.ipfs_hash;
            existingAsset.Type = type;

            return existingAsset;
        }

        public IEnumerable<ListingEntry> ScanCompletedListings(Dictionary<string, (string, int)> CompletedUTXOs)
        {
            //Want to chunk them because of how the SearchForUTXO's function works.
            foreach(var utxoChunk in Utils.ChunkItems(CompletedUTXOs.Keys, 100))
            {
                foreach(var completed in SearchForUTXOs(utxoChunk))
                {
                    var utxoDetails = CompletedUTXOs[completed.UTXO];
                    completed.Active = false;
                    completed.ExecutedTXID = utxoDetails.Item1;
                    completed.ExecutedBlock = utxoDetails.Item2;
                    yield return completed;
                }
            }
        }

        public IQueryable<ListingEntry> SearchForUTXOs(string[] utxoSet)
        {
            return _db.Listings
                .AsQueryable()
                .Where(l => l.Active && utxoSet.Contains(l.UTXO));
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
