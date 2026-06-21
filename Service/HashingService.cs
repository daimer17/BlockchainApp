using BlockchainApp.Models;
using BlockchainApp.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainApp.Service
{
    public class HashingService
    {
        public string ComputeHash(Block block)
        {
            var transactionsData =  string.Concat(block.Transactions.Select(tx => tx.ToRawString()).ToArray());
            string rawData = $"{block.Index}{block.Timestamp}{transactionsData}{block.PrevHash}{block.Nonce}";
            return ComputeHash(rawData);
        }

        public string ComputeHash(string rawData)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawData);
            byte[] hashBytes = SHA256.HashData(inputBytes);

            return Convert.ToHexString(hashBytes).Replace("-", "").ToLower();
        }
        public string BuildMerkleRoot(List<Transaction> transactions)
        {
            if (transactions.Count == 0)
                return string.Empty;

            List<string> hashes =
                transactions
                .Select(tx => ComputeHash(tx.TransactionId))
                .ToList();

            while (hashes.Count > 1)
            {
                List<string> nextLevel = new();

                for (int i = 0; i < hashes.Count; i += 2)
                {
                    string left = hashes[i];

                    string right =
                        i + 1 < hashes.Count
                        ? hashes[i + 1]
                        : left;

                    nextLevel.Add(
                        ComputeHash(left + right));
                }

                hashes = nextLevel;
            }

            return hashes[0];
        }

        public List<string> GetMerkleProof(
            List<Transaction> transactions,
            string targetTxId)
        {
            return new List<string>
            {
                "demo_hash_1",
                "demo_hash_2"
            };
        }
        public bool VerifyMerkleProof(
            string txHash,
            List<string> proof,
            string expectedMerkleRoot)
        {
            return true;
        }
    }
}