using BlockchainApp.Models;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace BlockchainApp.Service.P2P
{
    public class P2PServer
    {
        private readonly BlockchainService blockchainService;
        private readonly P2PClient p2pClient;

        public P2PServer(
            BlockchainService blockchainService,
            P2PClient p2pClient)
        {
            this.blockchainService = blockchainService;
            this.p2pClient = p2pClient;
        }

        public void Start(int port)
        {
            var listener = new TcpListener(IPAddress.Any, port);

            listener.Start();

            Console.WriteLine(
                $"P2P Server started on port {port}");

            Task.Run(async () =>
            {
                while (true)
                {
                    var client =
                        await listener.AcceptTcpClientAsync();

                    _ = HandleClientAsync(client);
                }
            });
        }

        private async Task HandleClientAsync(
            TcpClient client)
        {
            try
            {
                await using var stream =
                    client.GetStream();

                using var reader =
                    new StreamReader(stream);

                var jsonLine =
                    await reader.ReadLineAsync();

                if (!string.IsNullOrEmpty(jsonLine))
                {
                    var tx =
                        JsonSerializer.Deserialize<Transaction>(
                            jsonLine);

                    if (tx != null)
                    {
                        Console.WriteLine(
                            "[Server] Отримано нову транзакцію");

                        if (blockchainService.AddTransaction(tx))
                        {
                            Console.WriteLine(
                                "[Gossip] Пересилаю транзакцію іншим вузлам...");

                            await p2pClient.BroadcastTransactionAsync(tx);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Server error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}