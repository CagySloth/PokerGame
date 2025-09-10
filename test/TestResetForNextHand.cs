using System;
using System.Collections.Generic;
using System.Net;

public class TestResetForNextHand
{
    // === Copy of your server's GameState enum ===
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

    // === Server state variables ===
    private static List<Player> players = new List<Player>();
    private static List<IPEndPoint> clients = new List<IPEndPoint>();
    private static string[] communityCards = new string[5];
    private static int dealerPosition = 0;
    private static GameState currentGameState = GameState.PreFlop;
    private static string lastBroadcastMessage = "";

    // === Mock BroadcastMessage ===
    private static void BroadcastMessage(string message)
    {
        lastBroadcastMessage = message;
    }

    // === Function under test: ResetForNextHand() ===
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

    // === Test Runner ===
    public static void Main(string[] args)
    {
        Console.WriteLine("🧪 Running unit test for ResetForNextHand()\n");

        TestResetForNextHand_Basic();

        Console.WriteLine("\n✅ All tests passed!");
    }

    // --- Test: ResetForNextHand correctly resets state ---
    static void TestResetForNextHand_Basic()
    {
        Console.WriteLine("Test: ResetForNextHand() resets player and game state...");

        // Setup: 3 players with modified state
        IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);
        players.Clear();
        clients.Add(dummyEP);

        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 100, IsActive = false, HasActed = true });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 50, IsActive = true, HasActed = true });
        players.Add(new Player("Charlie", 3, dummyEP) { CurrentBet = 75, IsActive = false, HasActed = false });

        // Set game state
        communityCards = new[] { "A♠", "K♥", "Q♦", "J♣", "10♠" };
        dealerPosition = 1; // Bob is dealer
        currentGameState = GameState.Showdown;
        lastBroadcastMessage = "";

        // Call the function
        ResetForNextHand();

        // === Assertions ===

        // 1. Players: CurrentBet = 0
        foreach (var p in players)
        {
            Assert(p.CurrentBet == 0, $"{p.Name}.CurrentBet should be 0");
        }

        // 2. Players: IsActive = true
        foreach (var p in players)
        {
            Assert(p.IsActive == true, $"{p.Name}.IsActive should be true");
        }

        // 3. Players: HasActed = false
        foreach (var p in players)
        {
            Assert(p.HasActed == false, $"{p.Name}.HasActed should be false");
        }

        // 4. Community cards are null
        for (int i = 0; i < 5; i++)
        {
            Assert(communityCards[i] == null, $"communityCards[{i}] should be null");
        }

        // 5. Dealer position advanced: (1 + 1) % 3 = 2 → Charlie
        Assert(dealerPosition == 2, "Dealer position should advance to next player (Charlie)");

        // 6. Game state reset
        Assert(currentGameState == GameState.WaitingForPlayers, "Game state should be WaitingForPlayers");

        // 7. Broadcast message sent
        Assert(lastBroadcastMessage == "[RESET] Game reset. Waiting for next hand.",
               "Reset broadcast message should be sent");

        Console.WriteLine("✔️  PASS: ResetForNextHand() works correctly.\n");
    }

    // === Helper: Simple assertion ===
    static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Console.WriteLine($"❌ FAILED: {message}");
            Environment.Exit(1);
        }
    }
}