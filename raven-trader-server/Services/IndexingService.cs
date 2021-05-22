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
            List<RC_AssetVolume> volumeAdds = new List<RC_AssetVolume>();

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

            var currentBlocks = _rpc.GetNumBlocks();
            bool isFinished = (nextBlockNumber + 10) >= currentBlocks;
            //Anything within 10 blocks of head is considered "done" for logging

            for (numBlocks = 0; numBlocks < MaxPerTimer; numBlocks++)
            {
                //If we are all caught up, then don't process any more
                if (nextBlockNumber > currentBlocks) break;

                var blockIndexResults = IndexBlock(nextBlockNumber);

                blockAdds.Add(blockIndexResults.block);
                swapAdds.AddRange(blockIndexResults.swaps);
                volumeAdds.AddRange(blockIndexResults.volume);

                lastBlock = blockIndexResults.block;
                nextBlockNumber = lastBlock.Number + 1;
            }

            sw.Stop();

            if (blockAdds.Count > 0)
            {
                _db.Blocks.AddRange(blockAdds);
                _db.Swaps.AddRange(swapAdds);
                _db.AssetVolume.AddRange(volumeAdds);
                _db.SaveChanges();

                string extra = null;
                //Don't log time stats for every block
                if (!isFinished && sw.Elapsed.TotalSeconds != 0.00)
                {
                    extra = $"[ {numBlocks / sw.Elapsed.TotalSeconds} BPS ]";
                }

                _logger.LogInformation($"Saved. Current HEAD - {lastBlock?.Number ?? 0}/{currentBlocks}. Total Volume - {volumeAdds.Sum(av => av.TransactionVolume)} Assets {extra}");
            }
        }

        private (RC_Block block, List<RC_AssetVolume> volume, List<RC_Swap> swaps) IndexBlock(int blockNumber)
        {
            var blockHash = _rpc.GetBlockHash(blockNumber);
            dynamic blockDetails = _rpc.GetBlock(blockHash, 2);

            List<RC_Swap> foundSwaps = new List<RC_Swap>();

            Dictionary<string, RC_AssetVolume> assetVolumes = new Dictionary<string, RC_AssetVolume>();

            foreach (var block_tx in blockDetails.tx)
            {
                if (RC_Transaction.IsSwapTransaction(_rpc, block_tx, blockNumber, out RC_Swap swap))
                {
                    foundSwaps.Add(swap);

                    RC_AssetVolume vol = assetVolumes.ContainsKey(swap.AssetName) ? assetVolumes[swap.AssetName] : new RC_AssetVolume()
                    {
                        AssetName = swap.AssetName,
                        Block = blockNumber,
                    };
                    vol.SwapVolume += swap.Quantity;
                    assetVolumes[swap.AssetName] = vol;
                }

                //Swaps will also be picked up by this, no need to add them to the count after
                foreach (var vout in block_tx.vout)
                {
                    if (vout?.scriptPubKey?.type == "transfer_asset")
                    {
                        var asset_name = vout.scriptPubKey.asset.name.ToString();
                        var quantity = (float)vout.scriptPubKey.asset.amount;

                        RC_AssetVolume vol = assetVolumes.ContainsKey(asset_name) ? assetVolumes[asset_name] : new RC_AssetVolume()
                        {
                            AssetName = asset_name,
                            Block = blockNumber,
                        };

                        vol.TransactionVolume += quantity;
                        assetVolumes[asset_name] = vol;
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

            //_logger.LogInformation($"Indexed block {newBlock.Number} - {newBlock.Hash}");

            if (foundSwaps.Count > 0)
            {
                _logger.LogInformation($"Found {foundSwaps.Count} swaps on block {blockNumber} - {blockHash}");
            }

            return (newBlock, assetVolumes.Values.ToList(), foundSwaps);
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
