using BlockchainApp.Models;
using System.Net.Sockets;
using System.Text.Json;

namespace BlockchainApp.Service.P2P
{
    public class P2PClient
    {
        private readonly List<string> _peers;

        public P2PClient()
        {
            _peers = new List<string>();
        }

        public void Connect(string peerAddress)
        {
            if (!_peers.Contains(peerAddress))
            {
                _peers.Add(peerAddress);

                SavePeers();

                Console.WriteLine(
                    $"Connected to {peerAddress}");
            }
        }

        public async Task BroadcastTransactionAsync(Transaction transaction)
        {
            string jsonTransaction =
                JsonSerializer.Serialize(transaction);

            List<string> peersToRemove = new();

            foreach (var peer in _peers)
            {
                try
                {
                    var parts = peer.Split(':');

                    string ip = parts[0];
                    int port = int.Parse(parts[1]);

                    using var client = new TcpClient();

                    await client.ConnectAsync(ip, port);

                    await using var stream = client.GetStream();

                    await using var writer =
                        new StreamWriter(stream)
                        {
                            AutoFlush = true
                        };

                    await writer.WriteLineAsync(jsonTransaction);

                    Console.WriteLine($"Transaction sent to {peer}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[Мережа] Вузол {peer} вимкнений. Видаляємо зі списку пірів.");

                    Console.WriteLine(ex.Message);

                    peersToRemove.Add(peer);
                }
            }

            foreach (var peer in peersToRemove)
            {
                _peers.Remove(peer);
            }
           }

        private const string PeersFile = "peers.json";

        private void SavePeers()
        {
            string json = JsonSerializer.Serialize(_peers);
            File.WriteAllText(PeersFile, json);
        }
     
        public List<string> LoadPeers()
        {
            if (!File.Exists("peers.json"))
                return new List<string>();

            string json = File.ReadAllText("peers.json");

            var peers =
                JsonSerializer.Deserialize<List<string>>(json)
                ?? new List<string>();

            Console.WriteLine(
                $"Loaded {peers.Count} peer(s) from peers.json");

            return peers;
        }
    }
  }
