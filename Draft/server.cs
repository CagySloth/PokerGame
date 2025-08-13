// For P2P host to run, manage game logic and information.
// TODO: UDP error detection (not sure if it is needed), testing.

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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    public enum GameState
    {
        WaitingForPlayers,
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown,
    }

    public class Player
    {
        public string Name { get; set; }
        public int Chips { get; set; } = 1000;
        public int CurrentBet { get; set; }
        public string[] HoleCards { get; set; } = new string[2];
        public IPEndPoint EndPoint { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasActed { get; set; } = false;

        public Player(string name, IPEndPoint endpoint)
        {
            Name = name;
            EndPoint = endpoint;
        }
    }

    private static UdpClient udpServer;
    private static List<IPEndPoint> clients = new List<IPEndPoint>();
    private static List<Player> players = new List<Player>();
    private static List<string> deck = new List<string>();
    private static string[] communityCards = new string[5];
    private static int pot;
    private static int currentBet;
    private static int dealerPosition = 0;
    private static int currentPlayerTurn = 0;
    private static bool allBetsMatched = false;
    private static int smallBlindAmount = 10;
    private static int bigBlindAmount = 20;
    private static int lastRaiseAmount = 20;
    private static int smallBlindIndex = -1;
    private static int bigBlindIndex = -1;
    private static GameState currentGameState = GameState.WaitingForPlayers;
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Poker Server ===");

        Console.Write("Enter port number to listen on: ");
        int port = int.Parse(Console.ReadLine());

        udpServer = new UdpClient(port);
        Console.WriteLine($"Server started. Listening on port {port}.");

        Thread listenerThread = new Thread(ListenForMessages);
        listenerThread.Start();

        while (true)
        {
            Thread.Sleep(3000);
            if (players.Count >= 2)
            {
                break;
            }
        }

        StartNewGame();

        while (isRunning)
        {
            Console.WriteLine("Type 'exit' to shut down the server.");
            string command = Console.ReadLine();
            if (command.ToLower() == "exit")
            {
                isRunning = false;
                udpServer.Close();
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
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (!clients.Contains(remoteEP))
                {
                    clients.Add(remoteEP);
                    Console.WriteLine($"New connection from {remoteEP}");
                }

                HandleClientMessage(message, remoteEP);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving: {ex.Message}");
            }
        }
    }

    private static void HandleClientMessage(string message, IPEndPoint clientEndPoint)
    {
        string[] parts = message.Split(" ");
        if (parts.Length < 1) return;

        string command = parts[0];

        switch (command.ToLower())
        {
            case "join":
                if (parts.Length < 2) return;
                string playerName = parts[1];
                if (!players.Exists(p => p.Name == playerName))
                {
                    players.Add(new Player(playerName, clientEndPoint));
                    BroadcastMessage($"Player {playerName} joined.");
                    Console.WriteLine($"Player {playerName} joined.");
                }
                break;

            case "check":
                var playerCheck = players.Find(p => p.EndPoint.Equals(clientEndPoint));
                if (playerCheck != null && playerCheck.HasActed == false)
                {
                    HandlePlayerAction(playerCheck, "check");
                }
                break;

            case "call":
                var playerCall = players.Find(p => p.EndPoint.Equals(clientEndPoint));
                if (playerCall != null && playerCall.HasActed == false)
                {
                    HandlePlayerAction(playerCall, "call");
                }
                break;

            case "bet":
            case "raise":
                if (parts.Length < 3) return;
                var playerRaise = players.Find(p => p.EndPoint.Equals(clientEndPoint));
                if (playerRaise != null && playerRaise.HasActed == false)
                {
                    if (int.TryParse(parts[2], out int amount))
                    {
                        HandlePlayerAction(playerRaise, "raise", amount);
                    }
                }
                break;

            case "fold":
                var playerFold = players.Find(p => p.EndPoint.Equals(clientEndPoint));
                if (playerFold != null && playerFold.HasActed == false)
                {
                    HandlePlayerAction(playerFold, "fold");
                }
                break;

            case "allin":
                var playerAllIn = players.Find(p => p.EndPoint.Equals(clientEndPoint));
                if (playerAllIn != null && playerAllIn.HasActed == false)
                {
                    HandlePlayerAction(playerAllIn, "allin");
                }
                break;

            default:
                Console.WriteLine($"Unknown command: {message}");
                break;
        }
    }

    public static void StartNewGame()
    {
        ShuffleDeck();
        DealHoleCards();
        SetBlinds();

        pot = 0;
        currentBet = bigBlindAmount;
        lastRaiseAmount = bigBlindAmount;
        currentPlayerTurn = (bigBlindIndex + 1) % players.Count;
        currentGameState = GameState.PreFlop;

        BroadcastMessage("[GAME] Game started. Preflop betting begins.");
    }

    private static void RunBettingRound()
    {
        allBetsMatched = false;

        while (!allBetsMatched)
        {
            foreach (var player in players)
            {
                if (!player.IsActive || player.CurrentBet == currentBet || player.HasActed)
                    continue;

                // Simulate waiting for action via UDP
                Thread.Sleep(500); // Placeholder
            }

            CheckIfAllBetsMatched();
        }
    }

    private static void CheckIfAllBetsMatched()
    {
        allBetsMatched = true;
        foreach (var player in players)
        {
            if (player.IsActive && player.CurrentBet != currentBet)
            {
                allBetsMatched = false;
                break;
            }
        }
    }

    private static void ProceedToNextStage()
    {
        switch (currentGameState)
        {
            case GameState.PreFlop:
                RevealFlop();
                currentGameState = GameState.Flop;
                break;

            case GameState.Flop:
                RevealTurn();
                currentGameState = GameState.Turn;
                break;

            case GameState.Turn:
                RevealRiver();
                currentGameState = GameState.River;
                break;

            case GameState.River:
                currentGameState = GameState.Showdown;
                break;
        }
    }

    private static void RevealFlop()
    {
        communityCards[0] = deck[0]; deck.RemoveAt(0);
        communityCards[1] = deck[0]; deck.RemoveAt(0);
        communityCards[2] = deck[0]; deck.RemoveAt(0);
        BroadcastMessage($"[FLOP] {communityCards[0]}, {communityCards[1]}, {communityCards[2]}");
    }

    private static void RevealTurn()
    {
        communityCards[3] = deck[0]; deck.RemoveAt(0);
        BroadcastMessage($"[TURN] {communityCards[3]}");
    }

    private static void RevealRiver()
    {
        communityCards[4] = deck[0]; deck.RemoveAt(0);
        BroadcastMessage($"[RIVER] {communityCards[4]}");
    }

    private static void DetermineWinner()
    {
        var activePlayers = players.FindAll(p => p.IsActive);
        if (activePlayers.Count == 1)
        {
            var winner = activePlayers[0];
            winner.Chips += pot;
            BroadcastMessage($"[SHOWDOWN] {winner.Name} wins the pot of {pot} chips!");
            pot = 0;
            return;
        }

        Random rnd = new Random();
        var winnerRandom = activePlayers[rnd.Next(activePlayers.Count)]; // Logic missing
        winnerRandom.Chips += pot;
        BroadcastMessage($"[SHOWDOWN] {winnerRandom.Name} wins the pot of {pot} chips!");
        pot = 0;
    }

    private static void ResetForNextHand()
    {
        foreach (var player in players)
        {
            player.CurrentBet = 0;
            player.IsActive = true;
            player.HasActed = false;
        }

        communityCards = new string[5];
        dealerPosition = (dealerPosition + 1) % players.Count;
        currentGameState = GameState.WaitingForPlayers;
        BroadcastMessage("[RESET] Game reset. Waiting for next hand.");
    }

    private static void HandlePlayerAction(Player player, string action, int raiseAmount = 0)
    {
        switch (action.ToLower())
        {
            case "check":
                if (player.CurrentBet < currentBet)
                {
                    Console.WriteLine($"{player.Name} tried to check but needs to call.");
                    return;
                }
                BroadcastMessage($"[ACTION] {player.Name} checks.");
                player.HasActed = true;
                break;

            case "call":
                int callAmount = currentBet - player.CurrentBet;
                if (player.Chips < callAmount)
                {
                    callAmount = player.Chips;
                }
                player.Chips -= callAmount;
                player.CurrentBet += callAmount;
                pot += callAmount;
                BroadcastMessage($"[ACTION] {player.Name} calls {callAmount}.");
                player.HasActed = true;
                break;

            case "raise":
                if (raiseAmount < lastRaiseAmount)
                {
                    Console.WriteLine($"{player.Name} raised less than minimum.");
                    return;
                }

                int totalBet = currentBet + raiseAmount;
                int needed = totalBet - player.CurrentBet;

                if (player.Chips < needed)
                {
                    totalBet = player.Chips + player.CurrentBet;
                    needed = player.Chips;
                }

                player.Chips -= needed;
                player.CurrentBet += needed;
                pot += needed;
                currentBet = totalBet;
                lastRaiseAmount = raiseAmount;

                BroadcastMessage($"[ACTION] {player.Name} raises by {needed}.");

                foreach (var p in players)
                {
                    if (p != player && p.IsActive)
                        p.HasActed = false;
                }
                player.HasActed = true;
                break;

            case "fold":
                player.IsActive = false;
                BroadcastMessage($"[ACTION] {player.Name} folds.");
                player.HasActed = true;
                break;

            case "allin":
                int allInAmount = player.Chips;
                int totalAllInBet = player.CurrentBet + allInAmount;

                if (totalAllInBet > currentBet)
                {
                    if (totalAllInBet - player.CurrentBet < lastRaiseAmount)
                    {
                        Console.WriteLine($"{player.Name} went all-in below minimum raise.");
                    }
                    else
                    {
                        currentBet = totalAllInBet;
                        lastRaiseAmount = totalAllInBet - player.CurrentBet;
                    }
                }

                pot += allInAmount;
                player.CurrentBet += allInAmount;
                player.Chips = 0;
                BroadcastMessage($"[ACTION] {player.Name} goes all-in with {allInAmount}.");
                player.HasActed = true;
                break;
        }
    }

    private static void SetBlinds()
    {
        smallBlindIndex = (dealerPosition + 1) % players.Count;
        bigBlindIndex = (dealerPosition + 2) % players.Count;

        PlaceBlind(players[smallBlindIndex], smallBlindAmount);
        PlaceBlind(players[bigBlindIndex], bigBlindAmount);
    }

    private static void PlaceBlind(Player player, int amount)
    {
        if (player.Chips < amount)
        {
            amount = player.Chips;
        }

        player.Chips -= amount;
        player.CurrentBet += amount;
        pot += amount;

        BroadcastMessage($"[BLIND] {player.Name} posts {amount}.");
    }

    private static void DealHoleCards()
    {
        foreach (var player in players)
        {
            player.HoleCards[0] = deck[0];
            player.HoleCards[1] = deck[1];
            deck.RemoveAt(0);
            deck.RemoveAt(0);
        }
    }

    private static void ShuffleDeck()
    {
        string[] suits = { "♠", "♥", "♦", "♣" };
        string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        deck = new List<string>();
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                deck.Add(rank + suit);
            }
        }

        Random rng = new Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
    }

    private static void SendMessage(string message, IPEndPoint endPoint)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpServer.Send(data, data.Length, endPoint);
    }

    private static void BroadcastMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var client in clients)
        {
            udpServer.Send(data, data.Length, client);
        }
    }
}