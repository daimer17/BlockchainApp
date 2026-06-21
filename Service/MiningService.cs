using System;
using BlockchainApp.Models;
using BlockchainApp.Service;

namespace BlockсhainApp.Service
{
    public class MiningService
    {
        private readonly HashingService _hashingService;

        public MiningService(HashingService hashingService)
        {
            _hashingService = hashingService;
        }

        public long MineBlock(Block block, int difficulty)
        {
            block.DifficultyAtMining = difficulty;
            var target = new string('0', difficulty);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                block.Nonce++;
                block.Hash = _hashingService.ComputeHash(block);
                if (block.Nonce % 10000 == 0)
                {
                    Console.Write(".");
                }

                if (block.Hash.StartsWith(target))
                {
                    Console.WriteLine($"Block mined: {block.Hash} with nonce: {block.Nonce}");
                    stopwatch.Stop();
                    block.MiningDuratioSeconds = stopwatch.Elapsed.TotalSeconds;
                    return block.Nonce;
                }
            }
        }
    }
}