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
        public int ID { get; set; }
        public int Chips { get; set; } = 1000;
        public int CurrentBet { get; set; }
        public string[] HoleCards { get; set; } = new string[2];
        public IPEndPoint EndPoint { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasActed { get; set; } = false;

        public Player(string name, int id, IPEndPoint endpoint)
        {
            Name = name;
            ID = id;
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

        var playerScores = new List<(Player player, long score)>();
        foreach (var player in activePlayers)
        {
            long score = EvaluateHand(player.HoleCards, communityCards);
            playerScores.Add((player, score));
            Console.WriteLine($"{player.Name}: Score = {score,20:N0}");
        }

        playerScores.Sort((a, b) => b.score.CompareTo(a.score));
        long bestScore = playerScores[0].score;

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
            winners[0].Chips += remainder;
        }

        pot = 0;
    }

    // === Helper: GetCardRank ===
    private static int GetCardRank(string card)
    {
        if (string.IsNullOrEmpty(card)) return 0;

        // Remove suit by checking known endings
        if (card.EndsWith("♠") || card.EndsWith("♥") || card.EndsWith("♦") || card.EndsWith("♣"))
        {
            string rankPart = card.Substring(0, card.Length - 1);
            return rankPart switch
            {
                "10" => 10,
                "J" => 11,
                "Q" => 12,
                "K" => 13,
                "A" => 14,
                "2" => 2,
                "3" => 3,
                "4" => 4,
                "5" => 5,
                "6" => 6,
                "7" => 7,
                "8" => 8,
                "9" => 9,
                _ => 0
            };
        }
        return 0;
    }

    // === Helper: GetCardSuit ===
    private static char GetCardSuit(string card)
    {
        if (string.IsNullOrEmpty(card)) return '\0';
        if (card.EndsWith("♠")) return '♠';
        if (card.EndsWith("♥")) return '♥';
        if (card.EndsWith("♦")) return '♦';
        if (card.EndsWith("♣")) return '♣';
        return '\0';
    }

    // === Function under test: EvaluateHand() ===
    private static long EvaluateHand(string[] holeCards, string[] communityCards)
    {
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

        var uniqueRanks = ranks.Distinct().OrderByDescending(x => x).ToList();

        // --- Check for A-5-4-3-2 Wheel Straight ---
        if (uniqueRanks.Contains(14) && uniqueRanks.Contains(5) && uniqueRanks.Contains(4) && uniqueRanks.Contains(3) && uniqueRanks.Contains(2))
        {
            isStraight = true;
            highStraight = 5; // 5-high straight
        }
        // --- Check for any other 5 consecutive ranks ---
        else
        {
            for (int i = 0; i <= uniqueRanks.Count - 5; i++)
            {
                if (uniqueRanks[i] - uniqueRanks[i + 4] == 4)
                {
                    bool isConsecutive = true;
                    for (int j = 0; j < 4; j++)
                    {
                        if (uniqueRanks[i + j] - uniqueRanks[i + j + 1] != 1)
                        {
                            isConsecutive = false;
                            break;
                        }
                    }

                    if (isConsecutive)
                    {
                        isStraight = true;
                        highStraight = uniqueRanks[i];
                        break;
                    }
                }
            }
        }

        // Check for straight flush
        bool isStraightFlush = false;
        if (isFlush && isStraight)
        {
            char flushSuit = suitCounts.First(kv => kv.Value >= 5).Key;
            var straightRanks = new HashSet<int>();

            if (highStraight == 5 && isStraight) // Wheel straight
            {
                straightRanks.Add(14); // A
                straightRanks.Add(5);
                straightRanks.Add(4);
                straightRanks.Add(3);
                straightRanks.Add(2);
            }
            else
            {
                int current = highStraight;
                for (int i = 0; i < 5; i++)
                {
                    straightRanks.Add(current - i);
                }
            }

            int suitedInStraight = 0;
            foreach (string card in allCards)
            {
                int rank = GetCardRank(card);
                char suit = GetCardSuit(card);
                if (straightRanks.Contains(rank) && suit == flushSuit)
                {
                    suitedInStraight++;
                }
            }

            if (suitedInStraight >= 5)
            {
                isStraightFlush = true;
            }
        }

        int[] ofAKind = rankCounts.Values.OrderByDescending(x => x).ToArray();
        bool isFourOfAKind = ofAKind.Length >= 1 && ofAKind[0] == 4;
        bool isThreeOfAKind = ofAKind.Length >= 1 && ofAKind[0] == 3;
        bool isTwoPair = ofAKind.Length >= 2 && ofAKind[1] == 2;
        bool isPair = ofAKind.Length >= 1 && ofAKind[0] == 2;

        long score = 0;

        // Debug output
        string holeStr = $"{holeCards[0]} {holeCards[1]}";
        Console.WriteLine($"[DEBUG] {holeStr,8} | S={isStraight,-5} F={isFlush,-5} SF={isStraightFlush,-5} 4K={isFourOfAKind,-5}");

        // 1. Straight Flush
        if (isStraightFlush)
        {
            score = (10L << 56) | ((long)highStraight << 48);
        }
        // 2. Four of a Kind
        else if (isFourOfAKind)
        {
            int quadRank = rankCounts.First(kv => kv.Value == 4).Key;
            int kicker = ranks.Where(r => r != quadRank).Max();
            score = (9L << 56) | ((long)quadRank << 48) | ((long)kicker << 40);
        }
        // 3. Full House
        else if (isThreeOfAKind && isTwoPair)
        {
            int trips = rankCounts.First(kv => kv.Value == 3).Key;
            int pair = rankCounts.First(kv => kv.Value >= 2 && kv.Key != trips).Key;
            score = (8L << 56) | ((long)trips << 48) | ((long)pair << 40);
        }
        // 4. Flush
        else if (isFlush)
        {
            var flushRanks = allCards
                .Where(card => suitCounts[GetCardSuit(card)] >= 5)
                .Select(GetCardRank)
                .OrderByDescending(x => x)
                .Take(5);
            score = (7L << 56);
            var rankList = flushRanks.ToList();
            for (int i = 0; i < rankList.Count; i++)
            {
                score |= ((long)rankList[i]) << (48 - i * 8);
            }
        }
        // 5. Straight
        else if (isStraight)
        {
            score = (6L << 56) | ((long)highStraight << 48);
        }
        // 6. Three of a Kind
        else if (isThreeOfAKind)
        {
            int trips = rankCounts.First(kv => kv.Value == 3).Key;
            var kickers = ranks.Where(r => r != trips).Take(2).ToList();
            score = (5L << 56) | ((long)trips << 48) | ((long)kickers[0] << 40) | ((long)kickers[1] << 32);
        }
        // 7. Two Pair
        else if (isTwoPair)
        {
            var pairs = rankCounts.Where(kv => kv.Value == 2).OrderByDescending(kv => kv.Key).Take(2).ToList();
            int firstPair = pairs[0].Key;
            int secondPair = pairs[1].Key;
            int kicker = ranks.Where(r => r != firstPair && r != secondPair).Max();
            score = (4L << 56) | ((long)firstPair << 48) | ((long)secondPair << 40) | ((long)kicker << 32);
        }
        // 8. One Pair
        else if (isPair)
        {
            int pairRank = rankCounts.First(kv => kv.Value == 2).Key;
            var kickers = ranks.Where(r => r != pairRank).Take(3).ToList();
            score = (3L << 56)
                | ((long)pairRank << 48)
                | ((long)kickers[0] << 40)
                | ((long)kickers[1] << 32)
                | ((long)kickers[2] << 24);
        }
        // 9. High Card
        else
        {
            score = (2L << 56);
            for (int i = 0; i < 5; i++)
            {
                score |= ((long)ranks[i]) << (48 - i * 8);
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