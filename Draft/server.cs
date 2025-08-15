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

        // Evaluate each player's hand
        var playerScores = new List<(Player player, long score)>();
        foreach (var player in activePlayers)
        {
            long score = EvaluateHand(player.HoleCards, communityCards);
            playerScores.Add((player, score));
            Console.WriteLine($"{player.Name}: {player.HoleCards[0]} {player.HoleCards[1]} => Score: {score}");
        }

        // Sort by score descending
        playerScores.Sort((a, b) => b.score.CompareTo(a.score));
        long bestScore = playerScores[0].score;

        // Find all players with the best hand (handle ties)
        var winners = playerScores.Where(x => x.score == bestScore).Select(x => x.player).ToList();

        int totalPot = pot;
        int splitAmount = totalPot / winners.Count;
        int remainder = totalPot % winners.Count;

        if (winners.Count == 1)
        {
            var winner = winners[0];
            winner.Chips += splitAmount + remainder;
            BroadcastMessage($"[SHOWDOWN] {winner.Name} wins {splitAmount + remainder} chips with the best hand!");
        }
        else
        {
            string winnerNames = string.Join(" and ", winners.Select(w => w.Name));
            BroadcastMessage($"[SHOWDOWN] {winnerNames} split the pot! Each gets {splitAmount} chips.");
            foreach (var winner in winners)
            {
                winner.Chips += splitAmount;
            }
            // Add remainder to first winner
            winners[0].Chips += remainder;
        }

        pot = 0;
    }

    private static long EvaluateHand(string[] holeCards, string[] communityCards)
    {
        // Normal evaluation for short deck
        List<string> allCards = new List<string>();
        allCards.AddRange(holeCards);
        allCards.AddRange(communityCards);

        var ranks = allCards.Select(GetCardRank).OrderByDescending(x => x).ToList();
        var suits = allCards.Select(GetCardSuit).ToList();

        var rankCounts = ranks.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var suitCounts = suits.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

        bool isFlush = suitCounts.Values.Any(c => c >= 5);
        bool isStraight = false;
        int highStraight = 0;

        // Check for straight in short deck (10, J, Q, K, A)
        var uniqueRanks = ranks.Distinct().OrderByDescending(x => x).ToList();
        if (uniqueRanks.Contains(14) && uniqueRanks.Contains(13) && uniqueRanks.Contains(12) && uniqueRanks.Contains(11) && uniqueRanks.Contains(10))
        {
            isStraight = true;
            highStraight = 14; // Ace-high straight
        }

        // No wheel straight (A-5-4-3-2) because 5,4,3,2 not in deck

        // Four of a Kind, Full House, etc.
        int[] ofAKind = rankCounts.Values.OrderByDescending(x => x).ToArray();
        int fourOfAKind = ofAKind.Length >= 1 && ofAKind[0] == 4 ? 4 : 0;
        int threeOfAKind = ofAKind.Length >= 1 && ofAKind[0] == 3 ? 3 : 0;
        int pair = ofAKind.Length >= 1 && ofAKind[0] == 2 ? 2 : 0;
        int twoPair = ofAKind.Length >= 2 && ofAKind[1] == 2 ? 2 : 0;

        long score = 0;

        if (isStraight && isFlush)
        {
            score = (10_000_000L) | (highStraight << 16); // Straight flush
        }
        else if (fourOfAKind == 4)
        {
            int quadRank = rankCounts.First(kv => kv.Value == 4).Key;
            int kicker = ranks.Where(r => r != quadRank).Max();
            score = (9_900_000L) | (quadRank << 16) | (kicker << 8);
        }
        else if (threeOfAKind == 3 && twoPair >= 2)
        {
            int trips = rankCounts.First(kv => kv.Value == 3).Key;
            int pair = rankCounts.First(kv => kv.Value >= 2 && kv.Key != trips).Key;
            score = (9_800_000L) | (trips << 16) | (pair << 8);
        }
        else if (isFlush)
        {
            var flushRanks = allCards
                .Where(card => suitCounts[GetCardSuit(card)] >= 5)
                .Select(GetCardRank)
                .OrderByDescending(x => x)
                .Take(5);
            score = (9_700_000L);
            int shift = 16;
            foreach (int r in flushRanks)
            {
                score |= (r << shift);
                shift -= 8;
            }
        }
        else if (isStraight)
        {
            score = (9_600_000L) | (highStraight << 16);
        }
        else if (threeOfAKind == 3)
        {
            int trips = rankCounts.First(kv => kv.Value == 3).Key;
            var kickers = ranks.Where(r => r != trips).Take(2);
            score = (9_550_000L) | (trips << 16) | (kickers.First() << 8) | kickers.Last();
        }
        else if (twoPair == 2)
        {
            var pairs = rankCounts.Where(kv => kv.Value == 2).OrderByDescending(kv => kv.Key).Take(2);
            int firstPair = pairs.First().Key;
            int secondPair = pairs.Last().Key;
            int kicker = ranks.Where(r => r != firstPair && r != secondPair).Max();
            score = (9_540_000L) | (firstPair << 16) | (secondPair << 8) | kicker;
        }
        else if (pair == 2)
        {
            int pairRank = rankCounts.First(kv => kv.Value == 2).Key;
            var kickers = ranks.Where(r => r != pairRank).Take(3);
            score = (9_530_000L) | (pairRank << 16) | (kickers.ElementAt(0) << 8) | (kickers.ElementAt(1) << 4) | kickers.ElementAt(2);
        }
        else
        {
            score = (9_520_000L);
            for (int i = 0; i < 5; i++)
            {
                score |= (long)ranks[i] << (16 - i * 4);
            }
        }

        return score;
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
        var hands = GenerateTwoCardHands(players.Count);

    for (int i = 0; i < players.Count; i++)
    {
        players[i].HoleCards = hands[i];

        SendMessage($"[YOUR_CARDS] {hands[i][0]} {hands[i][1]}", players[i].EndPoint);
    }
    }

    private static void ShuffleDeck()
    {
        string[] suits = { "♠", "♥", "♦", "♣" };
        string[] ranks = { "10", "J", "Q", "K", "A" };

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

    private static List<string[]> GenerateTwoCardHands(int playerCount)
    {
        var hands = new List<string[]>();
        // Decide how many (2,7) hands to include
        // TODO: Confirm rules of deciding number of (2,7) hands
        Random rng = new Random();
        int numSpecialHands = rng.Next(0, Math.Min(2, playerCount + 1)); // 0, 1, or 2 (2,7) hands
        // Generate (2,7) hands
        for (int i = 0; i < numSpecialHands; i++)
        {
            hands.Add(new string[] { "2♠", "7♥" }); // You can randomize suits if desired
        }
        // Now generate normal two-card hands from the remaining deck
        int normalHandsNeeded = playerCount - numSpecialHands;
        var normalHands = new List<string[]>();
        // Build all possible two-card combos from the main deck
        var possibleHands = new List<string[]>();
        for (int i = 0; i < deck.Count; i++)
        {
            for (int j = i + 1; j < deck.Count; j++)
            {
                possibleHands.Add(new string[] { deck[i], deck[j] });
            }
        }
        // Shuffle possible hands
        for (int i = possibleHands.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (possibleHands[i], possibleHands[k]) = (possibleHands[k], possibleHands[i]);
        }
        // Take the first N normal hands
        for (int i = 0; i < normalHandsNeeded; i++)
        {
            hands.Add(possibleHands[i]);
        }
        // Shuffle final list so (2,7) hands are randomly distributed
        for (int i = hands.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (hands[i], hands[k]) = (hands[k], hands[i]);
        }

        return hands;
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