using System;
using System.Collections.Generic;
using SYstem.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    private static UdpClient udpServer;                                 // UDP server to handle communication
    private static List<IPEndPoint> clients = new List<IPEndPoint>();   // List of connected clients
    private static List<String> players = new List<string>();           // List of player names/id, to fetch data from DB
    private static List<int> playerChips = new List<int>();             // List of player chips
    private static List<string> deck = new List<string>();              // Card deck
    private string[] communityCards = new String[5];                    // Community cards
    private static int pot;                                             // Game pot
    private static int currentBet;                                      // Current bet to be matched
    private static List<int> playerBet;                                 // Player bets in current round
    private static List<int> playerOrder;                               // Randomize player order (index)
    private static int turn;                                            // Betting turn management
    private static int round;                                           // Player role management (Dealer/SB/BB)
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Poker Server ===");
        
        // Setup receiving port for client connections
        Console.WriteLine("Enter port number to listen on: ");
        int port = int.Parse(Console.ReadLine());

        // Start running server
        udpServer = new UdpClient(port);
        Console.WriteLine($"Server started. Listening on port {port}.");
        
        // Listen for client messages
        Thread listenerThread = new Thread(ListenForMessages);
        listenerThread.start();
        
        // Wait for players to join
        while (true)
        {
            sleep(3);
            // If 6 players have joined
            if (clients.length == 6)
            {
                break
            }
        }

        // Initialize game
        InitializeGame();

        while (isRunning)
        {
            Console.WriteLine("Type 'exit' to shut down the server.");
            string command = Console.ReadLine();
            if (command.ToLower() == "exit")
            {
                isRunning = false;
                udpServer.close();
                break;
            }
        }

        Console.WriteLine("Server shutting down.");
    }

    private static void ListenForMessages()
    {
        while (isRunning)
        {
            try
            {
                // Receive message from any connected client
                IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpServer.Receive(ref clientEndPoint);
                string message = Encoding.UTF8.GetString(receivedData);

                // If client is new, append to client list
                if (!clients.Contains(clientEndPoint))
                {
                    clients.Add(clientEndPoint);
                    Console.WriteLine($"New client connected: {clientEndPoint}");
                    // TODO: Fetch client data
                }

                Console.WriteLine($"Received from {clientEndPoint}: {Message}");
                HandleClientMessage(message, clientEndPoint); // Process received message
            }
            catch (SocketException)
            {
                Console.WriteLine("Error receiving message: SocketException.")
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error receiving message: {e.Message}");
            }
        }
    }

    private static void HandleClientMessage(string message, IPEndPoint clientEndPoint)
    {
        // Process message received from client
        string[] parts = message.Split(" ");
        string command = parts[0];

        switch (command.ToLower())
        {s
            case "join":
                // TODO: Mutex lock
                string playerName = parts[1]
                if (!players.Contains(playerName))
                {
                    players.Add(playerName);
                    playerChips.Add(1000);
                    playerBet.Add(0);
                    playerOrder.Add(players.Count - 1);
                    BroadcastMessage($"Player {playerName} joined.");
                    Console.WriteLine($"Player {playerName} joined.");
                }
                break;
            
            case "bet":
                break;

            case "fold":
                break;

            case "check":
                break;
            
            case "allin":
                break;
            
            default:
                // Handle unknown commands
                Console.WriteLine($"Unknown commend from {clientEndPoint}: {message}");
                SendMessage("Unknown command received.", clientEndPoint);
                break;
        }
    }
}