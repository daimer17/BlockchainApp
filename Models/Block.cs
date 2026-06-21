using System;
using System.Collections.Generic;

namespace BlockchainApp.Models
{
    public class Block
    {
        public int Index { get; set; }

        public List<Transaction> Transactions { get; set; }

        public string PrevHash { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public string MerkleRoot { get; set; } = string.Empty;

        public long Nonce { get; set; }

        public int DifficultyAtMining { get; set; }

        public DateTime Timestamp { get; set; }

        public double MiningDuratioSeconds { get; set; }

        public Block(int index, List<Transaction> transactions, string prevHash, DateTime timestamp)
        {
            Index = index;
            Transactions = transactions;
            PrevHash = prevHash;
            Timestamp = timestamp;
        }
    }
}