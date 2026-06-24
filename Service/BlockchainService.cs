using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using BlockchainApp.Models;
using BlockсhainApp.Service;


namespace BlockchainApp.Service
{
    public class BlockchainService
    {
        public List<Block> Chain { get; set; }
        private HashingService _hashingService;
        private MiningService _miningService;
        private decimal MiningReward = 50;
        public List<Transaction> PendingTransactions = new List<Transaction>();
        public Dictionary<string, decimal> BalancesState = new Dictionary<string, decimal>();
        public Dictionary<string,Dictionary<string, decimal>> TokenBalances = new();

        public int Difficulty { get; set; } = 1;
        public TimeSpan TransactionTtl = TimeSpan.FromMinutes(5);
        private readonly double _targetBlockTime = 1;
        private readonly int _adjustmentInterval = 5;
        
        private readonly List<Transaction> _pendingTransactions = new List<Transaction>();
        
        public BlockchainService(int difficulty)
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
            this.Difficulty = difficulty;
            AddGenesisBlock();
            LoadStateSnapshot();
        }

        private void AddGenesisBlock()
        {
            var block = new Block(0,new List<Transaction>(), "0", DateTime.Parse("2024-06-01T00:00:00Z"));
            Chain.Add(block);
        }
        private void AddBlock(List<Transaction> transactions)
        {
            var prevBlock = Chain.Last();

            var newBlock = new Block(
                prevBlock.Index + 1,
                transactions,
                prevBlock.Hash,
                DateTime.UtcNow
            );

            _miningService.MineBlock(newBlock, Difficulty);

            Chain.Add(newBlock);

            if (newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }
        }

        public decimal GetBalance(string publicKey)
        {
            decimal balance = 0;

            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.From == publicKey)
                        balance -= tx.Amount;

                    if (tx.To == publicKey)
                        balance += tx.Amount;
                }
            }

            return balance;
        }

        public int EvictStaleTransactions(int maxAgeSeconds)
        {
            int before = PendingTransactions.Count;

            PendingTransactions.RemoveAll(tx =>
                (DateTime.UtcNow - tx.TimeStamp).TotalSeconds >
                maxAgeSeconds);

            return before - PendingTransactions.Count;
        }

        public bool ValidateAndRebuildState()
        {
            BalancesState.Clear();

            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (!BalancesState.ContainsKey(tx.From))
                        BalancesState[tx.From] = 0;

                    if (!BalancesState.ContainsKey(tx.To))
                        BalancesState[tx.To] = 0;

                    if (tx.From != "System" && tx.From != "COINBASE")
                    {
                        BalancesState[tx.From] -= tx.Amount;

                        if (BalancesState[tx.From] < 0)
                        {
                            BalancesState.Clear();
                            return false;
                        }
                    }

                    BalancesState[tx.To] += tx.Amount;
                }
            }

            return true;
        }

        public bool AddTransaction(Transaction transaction)
        {
            var validation =
                TransactionService.ValidateTransaction(transaction);

            if (!validation.isValid)
            {
                Console.WriteLine(validation.error);
                return false;
            }

            int senderTxCount = PendingTransactions.Count(
                t => t.From == transaction.From);

            if (senderTxCount >= 3)
            {
                throw new InvalidOperationException(
                    "Spam detected.");
            }

            if (transaction.From != "COINBASE")
            {
                decimal balance = GetBalance(transaction.From);

                if (balance < transaction.Amount)
                {
                    Console.WriteLine("Недостатньо коштів.");
                    return false;
                }
            }

            PendingTransactions.Add(transaction);

            Console.WriteLine("Transaction added to mempool.");

            return true;
        }

        public void MinePendingTransactions(string minerPublicKey)
        {

            int removed = EvictStaleTransactions(60);

            if (removed > 0)
            {
                Console.WriteLine(
                    $"{removed} stale transaction(s) removed.");
            }

            var transactionsToInclude =
                PendingTransactions
                .Where(tx => tx.LockTime <= Chain.Count)
                .OrderByDescending(tx => tx.Amount)
                .Take(10)
                .ToList();

            var rewardTransaction =
                new Transaction(
                    "COINBASE",
                    minerPublicKey,
                    MiningReward);

            transactionsToInclude.Add(rewardTransaction);

            var lastBlock = Chain.Last();

            var newBlock = new Block(
                lastBlock.Index + 1,
                transactionsToInclude,
                lastBlock.Hash,
                DateTime.Now);

            _miningService.MineBlock(newBlock, Difficulty);

            Chain.Add(newBlock);

            PendingTransactions.RemoveAll(
                tx => transactionsToInclude.Contains(tx));

            UpdateBalances(newBlock);

            SaveStateSnapshot();

            AdjustDifficulty();
        }

        private void UpdateBalances(Block block)
        {
            foreach (var transaction in block.Transactions)
            {
                if (transaction.From != "COINBASE")
                {
                    if (!BalancesState.ContainsKey(transaction.From))
                    {
                        BalancesState[transaction.From] = 0;
                    }

                    BalancesState[transaction.From] -= transaction.Amount;
                }

                if (!BalancesState.ContainsKey(transaction.To))
                {
                    BalancesState[transaction.To] = 0;
                }

                BalancesState[transaction.To] += transaction.Amount;
            }
        }

        public void RebuildState()
        {
            BalancesState.Clear();

            foreach (var block in Chain)
            {
                UpdateBalances(block);
            }
        }

        public void SaveStateSnapshot()
        {
            string json =
                JsonSerializer.Serialize(
                    BalancesState,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText("state.json", json);

            Console.WriteLine("State saved.");
        }

        public void LoadStateSnapshot()
        {
            if (File.Exists("state.json"))
            {
                string json =
                    File.ReadAllText("state.json");

                BalancesState =
                    JsonSerializer.Deserialize<
                        Dictionary<string, decimal>>(json)
                    ?? new Dictionary<string, decimal>();

                Console.WriteLine("State loaded.");
            }
            else
            {
                Console.WriteLine(
                    "Snapshot not found. Rebuilding state...");

                RebuildState();
            }
        }

        public bool BroadcastTransactionFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found.");
                return false;
            }

            string json =
                File.ReadAllText(filePath);

            var tx =
                JsonSerializer.Deserialize<Transaction>(json);

            if (tx == null)
            {
                Console.WriteLine("Invalid file.");
                return false;
            }

            var rsaService =
                new RSAService();

            bool validSignature =
                rsaService.VerifySignature(
                    tx.ToRawString(),
                    tx.Signature,
                    tx.PublicKey);

            if (!validSignature)
            {
                Console.WriteLine(
                    "RSA signature invalid!");

                return false;
            }

            Console.WriteLine(
                "RSA signature verified.");

            return AddTransaction(tx);
        }

        private void AdjustDifficulty()
        {
            if (Chain.Count < _adjustmentInterval)
                return;

            var recentBlocks = Chain
                .Skip(Chain.Count - _adjustmentInterval)
                .Take(_adjustmentInterval)
                .ToList();

            double actualTime = recentBlocks.Sum(
                b => b.MiningDuratioSeconds);

            double expectedTime =
                _targetBlockTime * _adjustmentInterval;

            double newDifficulty =
                Difficulty * (expectedTime / actualTime);

            Difficulty = Math.Max(
                1,
                (int)Math.Round(newDifficulty));

            Console.WriteLine(
                $"Difficulty adjusted to {Difficulty}");

            Console.WriteLine(
                $"Expected time: {expectedTime:F2}s");

            Console.WriteLine(
                $"Actual time: {actualTime:F2}s");
        }
        public void AnalyzeChain()
        {
            Console.WriteLine("\n=== ANALYZE BLOCKCHAIN ===\n");

            bool hasErrors = false;

            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var prevBlock = Chain[i - 1];

                var recalculatedHash =
                    _hashingService.ComputeHash(currentBlock);

                if (currentBlock.Hash != recalculatedHash)
                {
                    Console.WriteLine(
                        $"Ошибка в блоке #{currentBlock.Index}: " +
                        $"Hash не соответствует данным блока " +
                        $"(Transactions/Timestamp/Nonce изменены).");

                    hasErrors = true;
                }

                if (!currentBlock.Hash.StartsWith(
                    new string('0', currentBlock.DifficultyAtMining)))
                {
                    Console.WriteLine(
                        $"Ошибка в блоке #{currentBlock.Index}: " +
                        $"Hash не соответствует Difficulty.");

                    hasErrors = true;
                }

                if (currentBlock.PrevHash != prevBlock.Hash)
                {
                    Console.WriteLine(
                        $"Ошибка в блоке #{currentBlock.Index}: " +
                        $"Цепочка разорвана " +
                        $"(PrevHash не совпадает с Hash предыдущего блока).");

                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                Console.WriteLine("Blockchain valid.");
            }

            Console.WriteLine();
        }

        public decimal GetTokenBalance(string address, string token)
        {
            if (!TokenBalances.ContainsKey(address))
                return 0;

            if (!TokenBalances[address].ContainsKey(token))
                return 0;

            return TokenBalances[address][token];
        }

        public bool TransferToken(string from, string to,decimal amount,string token,decimal fee)
        {
            decimal tokenBalance =
                GetTokenBalance(from, token);

            if (tokenBalance < amount)
            {
                Console.WriteLine(
                    $"Недостатньо {token}");

                return false;
            }

            decimal mainBalance =
                GetTokenBalance(from, "MAIN");

            if (mainBalance < fee)
            {
                Console.WriteLine(
                    "Недостатньо MAIN для комісії");

                return false;
            }

            TokenBalances[from][token] -= amount;

            if (!TokenBalances.ContainsKey(to))
            {
                TokenBalances[to] =
                    new Dictionary<string, decimal>();
            }

            if (!TokenBalances[to]
                .ContainsKey(token))
            {
                TokenBalances[to][token] = 0;
            }

            TokenBalances[to][token] += amount;

            TokenBalances[from]["MAIN"] -= fee;

            Console.WriteLine(
                $"Transfer success: {amount} {token}");

            return true;
        }

        public void MintToken(string owner, string token, decimal amount)
        {
            if (!TokenBalances.ContainsKey(owner))
                TokenBalances[owner] =
                    new Dictionary<string, decimal>();

            if (!TokenBalances[owner].ContainsKey(token))
                TokenBalances[owner][token] = 0;

            TokenBalances[owner][token] += amount;

            Console.WriteLine(
                $"Minted {amount} {token} to {owner}");
        }
        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var prevBlock = Chain[i - 1];
                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock))
                {
                    return false;
                }
                if (currentBlock.PrevHash != prevBlock.Hash)
                {
                    return false;
                }
            }
            return true;
        }
    }
}