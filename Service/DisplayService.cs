using BlockchainApp.Models;
using BlockchainApp.Service;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Transactions;

namespace BlockchainApp.Service
{
    public class DisplayService
    {
        public void DisplayBlockChain(List<Models.Block> chain)
        {
            foreach (var block in chain)
            {
                Console.WriteLine($"Index: {block.Index}");
                Console.WriteLine($"Timestamp: {block.Timestamp}");
                Console.WriteLine($"Hash: {block.Hash}");
                Console.WriteLine($"PrevHash: {block.PrevHash}");
                Console.WriteLine(new string('-', 50));

                var transactions = block.Transactions;
                foreach (var transaction in transactions)
                {
                    Console.WriteLine($"  Transaction ID: {transaction.Id}");
                    Console.WriteLine($"  From: {transaction.From}");
                    Console.WriteLine($"  To: {transaction.To}");
                    Console.WriteLine($"  Amount: {transaction.Amount}");
                    Console.WriteLine($"  Timestamp: {transaction.TimeStamp:()}");
                    Console.WriteLine(new string(' ', 4) + new string('-', 40));
                }      
            }
        }
    }
}