// For every player to run (including host), communicate between host (actual game logic and flow) and Unity methods.
// TODO: Implementation into Unity (calling Unity methods based on received message and send message based on player actions in Unity).

// Structure:
// Network Topology:
//
//                     P2P Host
//                        |
//      +--------+--------+--------+--------+
//      |        |        |        |        |
//  Client 1 Client 2 Client 3 Client 4 Client 5
//      |        |        |        |        |
//    Unity    Unity    Unity    Unity    Unity

// Description:
// - The Host is the central root node, managing communication with all Clients, including the Host itself.
// - All Clients are at the same  level and directly connected to the Host.
// - Each Client maintains an independent, bidirectional link to a Unity instance.
// - No inter-client communication is assumed. Each Client-Unity pair operates autonomously.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private static UdpClient udpClient;
    private static IPEndPoint serverEndpoint;
    private static string playerName;
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Poker Client ===");

        Console.Write("Enter server IP: ");
        string ip = Console.ReadLine();

        Console.Write("Enter server port: ");
        int port = int.Parse(Console.ReadLine());

        Console.Write("Enter your player name: ");
        playerName = Console.ReadLine();

        // Initialize UDP client
        udpClient = new UdpClient();
        serverEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);

        // Notify server of joining
        SendMessage($"join {playerName}");

        Console.WriteLine("Waiting for game updates...");

        // Start listening thread
        Thread listenThread = new Thread(ReceiveMessages);
        listenThread.Start();

        // Command input loop
        while (isRunning)
        {
            string command = Console.ReadLine().Trim().ToLower();

            if (command == "exit")
            {
                isRunning = false;
                break;
            }

            switch (command)
            {
                case "check":
                    SendMessage($"check {playerName}");
                    break;
                case "call":
                    SendMessage($"call {playerName}");
                    break;
                case "fold":
                    SendMessage($"fold {playerName}");
                    break;
                case "allin":
                    SendMessage($"allin {playerName}");
                    break;
                default:
                    if (command.StartsWith("raise") || command.StartsWith("bet"))
                    {
                        string[] parts = command.Split(' ');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int amount))
                        {
                            SendMessage($"raise {playerName} {amount}");
                        }
                        else
                        {
                            Console.WriteLine("Invalid raise format. Use: 'raise 50'");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unknown command. Try: check, call, fold, raise <amount>, allin");
                    }
                    break;
            }
        }

        udpClient.Close();
        Console.WriteLine("Client shutting down.");
    }

    private static void SendMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, serverEndpoint);
    }

    private static void ReceiveMessages()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(receivedBytes);

                Console.WriteLine($">> {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                isRunning = false;
            }
        }
    }
}