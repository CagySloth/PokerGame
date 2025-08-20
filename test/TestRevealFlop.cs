using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

public class TestRevealFlop
{
    // === Copy of your server state (must match exactly) ===
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

    // === Game state variables (static, as in server.cs) ===
    private static List<string> deck = new List<string>();
    private static string[] communityCards = new string[5]; // Index 0,1,2 = flop
    private static List<IPEndPoint> clients = new List<IPEndPoint>();
    private static bool isRunning = true;

    // === Mock BroadcastMessage (just logs instead of sending) ===
    private static string lastBroadcastMessage = "";

    private static void BroadcastMessage(string message)
    {
        lastBroadcastMessage = message;
        // In real server: sends to UDP clients
    }

    // === Function under test: RevealFlop() ===
    private static void RevealFlop()
    {
        communityCards[0] = deck[0]; deck.RemoveAt(0);
        communityCards[1] = deck[0]; deck.RemoveAt(0);
        communityCards[2] = deck[0]; deck.RemoveAt(0);
        BroadcastMessage($"[FLOP] {communityCards[0]}, {communityCards[1]}, {communityCards[2]}");
    }

    // === Test Runner ===
    public static void Main(string[] args)
    {
        Console.WriteLine("🧪 Running unit tests for RevealFlop()\n");

        TestFlopRevealsThreeCards();
        TestDeckSizeReducedByThree();
        TestBroadcastMessageFormat();
        TestCommunityCardsAreNotNull();

        Console.WriteLine("\n✅ All RevealFlop() tests passed!");
    }

    // --- Test 1: Three cards are correctly placed in communityCards ---
    static void TestFlopRevealsThreeCards()
    {
        Console.WriteLine("Test 1: RevealFlop() sets communityCards[0..2]...");
        ResetState();

        // Setup: 5-card deck
        deck.AddRange(new[] { "A♠", "K♥", "Q♦", "J♣", "10♠" });

        RevealFlop();

        Assert(communityCards[0] == "A♠", "Flop card 1 should be A♠");
        Assert(communityCards[1] == "K♥", "Flop card 2 should be K♥");
        Assert(communityCards[2] == "Q♦", "Flop card 3 should be Q♦");
        Console.WriteLine("✔️  PASS: Flop cards assigned correctly.\n");
    }

    // --- Test 2: Deck size is reduced by exactly 3 ---
    static void TestDeckSizeReducedByThree()
    {
        Console.WriteLine("Test 2: Deck size reduced by 3...");
        ResetState();

        deck.AddRange(new[] { "A♠", "K♥", "Q♦", "J♣", "10♠", "9♠" }); // 6 cards
        int initialSize = deck.Count;

        RevealFlop();

        Assert(deck.Count == initialSize - 3, $"Deck size should be {initialSize - 3}, but was {deck.Count}");
        Console.WriteLine("✔️  PASS: Deck reduced by 3 cards.\n");
    }

    // --- Test 3: Broadcast message is correct ---
    static void TestBroadcastMessageFormat()
    {
        Console.WriteLine("Test 3: Broadcast message format...");
        ResetState();
        lastBroadcastMessage = "";

        deck.AddRange(new[] { "7♠", "8♥", "9♦", "X", "X" });

        RevealFlop();

        string expected = "[FLOP] 7♠, 8♥, 9♦";
        Assert(lastBroadcastMessage == expected, 
            $"Broadcast should be '{expected}', but was '{lastBroadcastMessage}'");
        Console.WriteLine("✔️  PASS: Broadcast message correct.\n");
    }

    // --- Test 4: No nulls in community cards ---
    static void TestCommunityCardsAreNotNull()
    {
        Console.WriteLine("Test 4: Community cards are not null...");
        ResetState();

        deck.AddRange(new[] { "J♠", "Q♥", "K♦" });

        RevealFlop();

        Assert(communityCards[0] != null, "communityCards[0] is null");
        Assert(communityCards[1] != null, "communityCards[1] is null");
        Assert(communityCards[2] != null, "communityCards[2] is null");
        Console.WriteLine("✔️  PASS: No null flop cards.\n");
    }

    // --- Test 5: Exception if deck has fewer than 3 cards ---
    static void TestThrowsIfNotEnoughCards()
    {
        Console.WriteLine("Test 5: Should throw if deck < 3 cards...");
        ResetState();

        deck.AddRange(new[] { "A♠", "K♥" }); // Only 2 cards

        bool threwException = false;
        try
        {
            RevealFlop();
        }
        catch (ArgumentOutOfRangeException)
        {
            threwException = true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Unexpected exception: {e.GetType()}");
        }

        Assert(threwException, "Expected ArgumentOutOfRangeException when deck < 3");
        Console.WriteLine("✔️  PASS: Correctly throws when not enough cards.\n");
    }

    // === Helper Methods ===
    static void ResetState()
    {
        deck.Clear();
        communityCards = new string[5];
        lastBroadcastMessage = "";
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Console.WriteLine($"❌ FAILED: {message}");
            Environment.Exit(1);
        }
    }
}