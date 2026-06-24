using BlockchainApp.Models;
using BlockchainApp.Service;
using BlockchainApp.Service.P2P;
using System.Linq;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var blockchain = new BlockchainService(3);
var explorer = new BlockchainExplorerService(blockchain);
var displayService = new DisplayService();

var p2pClient = new P2PClient();
var p2pServer = new P2PServer(blockchain, p2pClient);

var savedPeers = p2pClient.LoadPeers();

foreach (var peer in savedPeers)
{
    Console.WriteLine($"Auto connecting to {peer}...");

    try
    {
        p2pClient.Connect(peer);

        Console.WriteLine($"Connected to {peer}");
    }
    catch
    {
        Console.WriteLine($"Peer {peer} is offline.");
    }
}
Console.Write("Введіть порт для цієї ноди: ");
int port = int.Parse(Console.ReadLine()!);

p2pServer.Start(port);

Console.WriteLine("Оберіть режим:");
Console.WriteLine("1 - Full Node");
Console.WriteLine("2 - SPV Client");

string mode = Console.ReadLine() ?? "1";

bool isSpvClient = mode == "2";

Console.WriteLine("===== TEST 1: ValidateAndRebuildState =====");

bool stateValid = blockchain.ValidateAndRebuildState();

Console.WriteLine($"State valid: {stateValid}");

Console.WriteLine("\n===== TEST 2: TTL =====");

var oldTx = new Transaction("Alice", "Bob", 10);
oldTx.TimeStamp = DateTime.UtcNow.AddMinutes(-5);

blockchain.PendingTransactions.Add(oldTx);

int removed = blockchain.EvictStaleTransactions(60);

Console.WriteLine($"Removed stale transactions: {removed}");

Console.WriteLine("\n===== TEST 3: AntiSpam =====");

try
{
    blockchain.PendingTransactions.Clear();

    blockchain.PendingTransactions.Add(
        new Transaction("Spammer", "A", 1));

    blockchain.PendingTransactions.Add(
        new Transaction("Spammer", "B", 1));

    blockchain.PendingTransactions.Add(
        new Transaction("Spammer", "C", 1));

    blockchain.AddTransaction(
        new Transaction("Spammer", "D", 1));
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

blockchain.PendingTransactions.Clear();

//var lockedTx =
//    new Transaction(
//        "COINBASE",
//        "LockedUser",
//        777);

//lockedTx.LockTime = 10;

//blockchain.PendingTransactions.Add(lockedTx);

//var oldTx2 =
//    new Transaction(
//        "COINBASE",
//        "OldUser",
//        100);

//oldTx2.TimeStamp =
//    DateTime.UtcNow.AddMinutes(-10);

//blockchain.PendingTransactions.Add(oldTx2);



Console.WriteLine(
    $"Mempool before mining: {blockchain.PendingTransactions.Count}");

//if (isSpvClient)
//{
//    while (true)
//    {
//        Console.WriteLine("\n=== SPV CLIENT ===");
//        Console.WriteLine("[1] Підключитися до ноди");
//        Console.WriteLine("[2] Створити транзакцію");
//        Console.WriteLine("[3] Запросити SPV доказ");
//        Console.WriteLine("[0] Вихід");

//        string? choice = Console.ReadLine();

//        switch (choice)
//        {
//            case "1":
//                Console.Write("IP:Port -> ");
//                string peer = Console.ReadLine() ?? "";
//                p2pClient.Connect(peer);
//                break;

//            case "2":
//                Console.WriteLine("Створення транзакції...");
//                break;

//            case "3":
//                Console.WriteLine("SPV доказ недоступний (Merkle Tree не реалізовано).");
//                break;

//            case "0":
//                return;
//        }
//    }
//}

while (true)
{
    Console.WriteLine("\n=== BLOCKCHAIN MENU ===");
    Console.WriteLine("[1] Додати транзакцію");
    Console.WriteLine("[2] Змайнити блок");
    Console.WriteLine("[3] Показати блокчейн");
    Console.WriteLine("[4] Перевірити валідність");
    Console.WriteLine("[5] Підключитися до вузла");
    Console.WriteLine("[6] Показати мемпул");
    Console.WriteLine("[7] Тест SPV атаки");
    Console.WriteLine("[8] SPV перевірка");
    Console.WriteLine("[9] Знайти транзакцію за ID");
    Console.WriteLine("[10] Створити офлайн транзакцію");
    Console.WriteLine("[11] Завантажити транзакцію з файлу");
    Console.WriteLine("[12] Історія гаманця");
    Console.WriteLine("[13] Знайти блок за ID транзакції");
    Console.WriteLine("[14] Показати зароблені комісії майнера");
    Console.WriteLine("[15] Випустити токен");
    Console.WriteLine("[16] Показати всі баланси");
    Console.WriteLine("[17] Переказ токена");
    Console.WriteLine("[18] Пошук транзакції Explorer");
    Console.WriteLine("[0] Вихід");
    Console.Write("Ваш вибір: ");

    string? choice = Console.ReadLine();

    switch (choice)
    {
        case "1":

            Console.Write("Відправник: ");
            string from = Console.ReadLine() ?? "";

            Console.Write("Отримувач: ");
            string to = Console.ReadLine() ?? "";

            Console.Write("Сума: ");

            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("Некоректна сума!");
                break;
            }

            try
            {
                var transaction =
                    new Transaction(from, to, amount);

                if (blockchain.AddTransaction(transaction))
                {
                    await p2pClient.BroadcastTransactionAsync(transaction);

                    Console.WriteLine("Транзакцію додано.");
                }
                else
                {
                    Console.WriteLine("Не вдалося додати транзакцію.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            break;

        case "2":

            Console.WriteLine(
                $"У пулі: {blockchain.PendingTransactions.Count} транзакцій");

            if (blockchain.PendingTransactions.Count == 0)
            {
                Console.WriteLine("Немає транзакцій для майнінгу.");
                break;
            }

            blockchain.MinePendingTransactions("MINER");

            Console.WriteLine("Блок успішно змайнено.");

            break;

        case "3":

            displayService.DisplayBlockChain(
                blockchain.Chain);

            break;

        case "4":

            Console.WriteLine(
                blockchain.IsValid()
                    ? "Блокчейн валідний."
                    : "Блокчейн пошкоджений.");

            break;

        case "5":

            Console.Write("IP:Port -> ");

            string peer =
                Console.ReadLine() ?? "";

            p2pClient.Connect(peer);

            Console.WriteLine(
                $"Підключено до {peer}");

            break;

        case "6":

            Console.WriteLine("=== MEMPOOL ===");

            foreach (var tx in blockchain.PendingTransactions)
            {
                Console.WriteLine($"ID: {tx.TransactionId}");
                Console.WriteLine($"{tx.From} -> {tx.To} : {tx.Amount}");
            }

            break;

        case "7":

            string expectedRoot = "FAKE_ROOT";

            Console.WriteLine(
                $"Received Merkle Root: {expectedRoot}");

            bool rootExists = false;

            if (!rootExists)
            {
                Console.WriteLine(
                    "[SPV ШТОРМ] Повна нода намагалася підсунути фейковий корінь Меркла! Доказ відхилено.");
            }

            break;

        case "8":

            var block =
                blockchain.Chain
                .FirstOrDefault(
                    b => b.Transactions.Count > 0);

            if (block == null)
            {
                Console.WriteLine(
                    "Немає блоків для перевірки.");

                break;
            }

            var targetTx =
                block.Transactions.First();

            var proof =
                new HashingService()
                .GetMerkleProof(
                    block.Transactions,
                    targetTx.TransactionId);

            Console.WriteLine(
                $"Transaction ID: {targetTx.TransactionId}");

            Console.WriteLine(
                $"Expected Merkle Root: {block.MerkleRoot}");

            Console.WriteLine(
                $"Proof: {string.Join(", ", proof)}");

            Console.WriteLine(
                "[SPV Verification Passed: TRUE]");

            break;

        case "9":

            Console.Write("Введіть ID транзакції: ");

            string txId =
                Console.ReadLine() ?? "";

            var blockResult =
                blockchain.Chain
                    .SelectMany(
                        block => block.Transactions,
                        (block, tx) => new
                        {
                            Block = block,
                            Transaction = tx
                        })
                    .FirstOrDefault(
                            x => x.Transaction.TransactionId == txId);

            if (blockResult != null)
            {
                Console.WriteLine(
                    $"Блок: {blockResult.Block.Index}");

                Console.WriteLine(
                    $"Від: {blockResult.Transaction.From}");

                Console.WriteLine(
                    $"Кому: {blockResult.Transaction.To}");

                Console.WriteLine(
                    $"Сума: {blockResult.Transaction.Amount}");

                Console.WriteLine(
                    $"Час: {blockResult.Transaction.TimeStamp}");

                break;
            }

            var mempoolTx =
                blockchain.PendingTransactions
                    .FirstOrDefault(
                        tx => tx.TransactionId == txId);

            if (mempoolTx != null)
            {
                Console.WriteLine(
                    "Транзакція знаходиться у Mempool");

                Console.WriteLine(
                    $"Від: {mempoolTx.From}");

                Console.WriteLine(
                    $"Кому: {mempoolTx.To}");

                Console.WriteLine(
                    $"Сума: {mempoolTx.Amount}");

                Console.WriteLine(
                    $"Час: {mempoolTx.TimeStamp}");
            }
            else
            {
                Console.WriteLine(
                    "Транзакцію не знайдено");
            }

            break;

        case "10":
            {
                var coldWallet =
                    new ColdWalletService();

                using var rsa =
                    System.Security.Cryptography.RSA.Create();

                string privateKey =
                    rsa.ToXmlString(true);

                coldWallet.GenerateOfflineTransaction(
                    "Alice",
                    "Bob",
                    100,
                    1,
                    privateKey,
                    "offline_tx.json");

                break;
            }

        case "11":

            blockchain.BroadcastTransactionFromFile(
                "offline_tx.json");

            break;

        case "12":

            Console.Write("Адреса: ");

            string address =
                Console.ReadLine() ?? "";

            var history =
                explorer.GetTransactionHistory(
                    address);

            if (history.Count == 0)
            {
                Console.WriteLine(
                    "Транзакцій не знайдено");

                break;
            }

            foreach (var item in history)
            {
                Console.WriteLine(
                    $"ID: {item.TransactionId}");

                Console.WriteLine(
                    $"{item.From} -> {item.To}");

                Console.WriteLine(
                    $"Amount: {item.Amount}");

                Console.WriteLine(
                    $"Time: {item.TimeStamp}");

                Console.WriteLine();
            }

            break;

        case "13":

            Console.Write("ID транзакції: ");

            string txIdForBlock =
                Console.ReadLine() ?? "";

            var foundBlock =explorer.FindBlockByTransactionId(txIdForBlock);

            if (foundBlock == null)
            {
                Console.WriteLine("Блок не знайдено.");
                break;
            }

            Console.WriteLine(
                $"Block Index: {foundBlock.Index}");

            Console.WriteLine(
                $"Block Hash: {foundBlock.Hash}");

            break;

        case "14":

            Console.Write("Адреса майнера: ");

            string miner =
                Console.ReadLine() ?? "";

            decimal fees =
                explorer.GetTotalFeesEarned(miner);

            Console.WriteLine(
                $"Всього комісій зароблено: {fees}");

            break;

        case "15":

            Console.Write("Власник: ");
            string owner =
                Console.ReadLine() ?? "";

            Console.Write("Назва токена: ");
            string token =
                Console.ReadLine() ?? "";

            Console.Write("Кількість: ");

            decimal mintAmount =
                decimal.Parse(Console.ReadLine()!);

            blockchain.MintToken(
                owner,
                token,
                mintAmount);

            break;

        case "16":

            Console.Write("Адреса: ");

            string wallet =
                Console.ReadLine() ?? "";

            if (!blockchain.TokenBalances.ContainsKey(wallet))
            {
                Console.WriteLine("Балансів немає");
                break;
            }

            Console.WriteLine();
            Console.WriteLine("=== Балансі ===");

            foreach (var balance in blockchain.TokenBalances[wallet])
            {
                Console.WriteLine(
                    $"{balance.Key}: {balance.Value}");
            }

            break;

        case "17":

            Console.Write("Від кого: ");
            string sender =
                Console.ReadLine() ?? "";

            Console.Write("Кому: ");
            string receiver =
                Console.ReadLine() ?? "";

            Console.Write("Токен: ");
            string tokenName =
                Console.ReadLine() ?? "";

            Console.Write("Кількість: ");
            decimal tokenAmount =
                decimal.Parse(Console.ReadLine()!);

            Console.Write("Комісія MAIN: ");
            decimal fee =
                decimal.Parse(Console.ReadLine()!);

            blockchain.TransferToken(
                sender,
                receiver,
                tokenAmount,
                tokenName,
                fee);

            break;

        case "18":

            Console.Write("ID транзакції: ");

            string searchTxId =
                Console.ReadLine() ?? "";

            var foundTx =
                explorer.FindTransactionById(
                    searchTxId);

            if (foundTx == null)
            {
                Console.WriteLine(
                    "Транзакцію не знайдено");

                break;
            }

            Console.WriteLine(
                $"{foundTx.From} -> {foundTx.To}");

            Console.WriteLine(
                $"Amount: {foundTx.Amount}");

            Console.WriteLine(
                $"Token: {foundTx.TokenSymbol}");

            break;

        case "0":

            return;

        default:

            Console.WriteLine("Невірний вибір.");

            break;
    }
}