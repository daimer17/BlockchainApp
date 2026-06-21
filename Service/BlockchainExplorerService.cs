using BlockchainApp.Models;

namespace BlockchainApp.Service
{
    public class BlockchainExplorerService
    {
        private readonly BlockchainService _blockchain;

        public BlockchainExplorerService(
            BlockchainService blockchain)
        {
            _blockchain = blockchain;
        }

        public Transaction? FindTransactionById(string txId)
        {
            var tx = _blockchain.Chain
                .SelectMany(b => b.Transactions)
                .FirstOrDefault(t => t.TransactionId == txId);

            if (tx != null)
                return tx;

            return _blockchain.PendingTransactions
                .FirstOrDefault(t => t.TransactionId == txId);
        }

        public Block? FindBlockByTransactionId(string txId)
        {
            return _blockchain.Chain
                .FirstOrDefault(b =>
                    b.Transactions.Any(
                        t => t.TransactionId == txId));
        }

        public List<Transaction> GetTransactionHistory(
            string address)
        {
            return _blockchain.Chain
                .SelectMany(b => b.Transactions)
                .Where(t =>
                    t.From == address ||
                    t.To == address)
                .OrderByDescending(t => t.TimeStamp)
                .ToList();
        }

        public decimal GetTotalFeesEarned(string minerAddress)
        {
            decimal total = 0;

            foreach (var block in _blockchain.Chain)
            {
                bool minedByMiner =
                    block.Transactions.Any(
                        t => t.From == "COINBASE"
                          && t.To == minerAddress);

                if (minedByMiner)
                {
                    total += block.Transactions.Sum(t => t.Fee);
                }
            }

            return total;
        }
    }
}