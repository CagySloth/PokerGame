using System;
using System.Collections.Generic;
using System.Net;

public class TestSetBlindsAndPlaceBlind
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

        public override string ToString() => Name;
    }

    // === Server state variables ===
    private static List<Player> players = new List<Player>();
    private static int pot = 0;
    private static int currentBet = 0;
    private static int dealerPosition = 0;
    private static int smallBlindAmount = 10;
    private static int bigBlindAmount = 20;
    private static int smallBlindIndex = -1;
    private static int bigBlindIndex = -1;
    private static string lastBroadcastMessage = "";

    // === Mock BroadcastMessage ===
    private static void BroadcastMessage(string message)
    {
        lastBroadcastMessage = message;
    }

    // === Function under test: SetBlinds() ===
    private static void SetBlinds()
    {
        smallBlindIndex = (dealerPosition + 1) % players.Count;
        bigBlindIndex = (dealerPosition + 2) % players.Count;

        PlaceBlind(players[smallBlindIndex], smallBlindAmount);
        PlaceBlind(players[bigBlindIndex], bigBlindAmount);
    }

    // === Function under test: PlaceBlind() ===
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

    // === Test Runner ===
    public static void Main(string[] args)
    {
        Console.WriteLine("🧪 Running unit tests for SetBlinds() and PlaceBlind()\n");

        TestSetBlinds_CorrectPositions();
        TestPlaceBlind_NormalAmount();
        TestPlaceBlind_AllInDueToLowChips();

        Console.WriteLine("\n✅ All tests passed!");
    }

    // --- Test 1: SetBlinds assigns correct SB and BB positions ---
    static void TestSetBlinds_CorrectPositions()
    {
        Console.WriteLine("Test 1: SetBlinds() assigns correct SB and BB positions...");
        ResetState();

        // Setup: 4 players, dealer is Alice (index 0)
        IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);
        players.Add(new Player("Alice", 1, dummyEP)); // Dealer
        players.Add(new Player("Bob", 2, dummyEP));
        players.Add(new Player("Charlie", 3, dummyEP));
        players.Add(new Player("Diana", 4, dummyEP));

        dealerPosition = 0;

        SetBlinds();

        // SB = (0 + 1) % 4 = 1 → Bob
        // BB = (0 + 2) % 4 = 2 → Charlie
        Assert(smallBlindIndex == 1, "SB index should be 1 (Bob)");
        Assert(bigBlindIndex == 2, "BB index should be 2 (Charlie)");

        Assert(players[smallBlindIndex].Name == "Bob", "Bob should be SB");
        Assert(players[bigBlindIndex].Name == "Charlie", "Charlie should be BB");

        Console.WriteLine("✔️  PASS: SB/BB positions assigned correctly.\n");
    }

    // --- Test 2: PlaceBlind deducts chips and updates pot ---
    static void TestPlaceBlind_NormalAmount()
    {
        Console.WriteLine("Test 2: PlaceBlind() with normal amount...");
        ResetState();

        IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);
        var player = new Player("Alice", 1, dummyEP) { Chips = 1000 };
        players.Add(player);

        pot = 0;
        lastBroadcastMessage = "";

        PlaceBlind(player, 20);

        Assert(player.Chips == 980, "Player should have 980 chips after posting 20");
        Assert(player.CurrentBet == 20, "Player.CurrentBet should be 20");
        Assert(pot == 20, "Pot should increase by 20");
        Assert(lastBroadcastMessage == "[BLIND] Alice posts 20.", "Correct blind message broadcasted");

        Console.WriteLine("✔️  PASS: Normal blind posted correctly.\n");
    }

    // --- Test 3: PlaceBlind caps amount if player doesn't have enough chips ---
    static void TestPlaceBlind_AllInDueToLowChips()
    {
        Console.WriteLine("Test 3: PlaceBlind() handles all-in due to low chips...");
        ResetState();

        IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);
        var player = new Player("Bob", 2, dummyEP) { Chips = 7 }; // Less than blind
        players.Add(player);

        pot = 0;
        lastBroadcastMessage = "";

        PlaceBlind(player, 20); // Tries to post 20

        Assert(player.Chips == 0, "Player should go all-in with 7 chips");
        Assert(player.CurrentBet == 7, "Player.CurrentBet should be 7");
        Assert(pot == 7, "Pot should only increase by 7");
        Assert(lastBroadcastMessage == "[BLIND] Bob posts 7.", "Message should reflect actual amount");

        Console.WriteLine("✔️  PASS: All-in blind handled correctly.\n");
    }

    // --- Test 4: SetBlinds with wrap-around dealer ---
    static void TestSetBlinds_WrapAround()
    {
        Console.WriteLine("Test 4: SetBlinds() handles wrap-around (last players)...");
        ResetState();

        IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);
        players.Add(new Player("Alice", 1, dummyEP));
        players.Add(new Player("Bob", 2, dummyEP));
        players.Add(new Player("Charlie", 3, dummyEP));

        dealerPosition = 2; // Charlie is dealer

        SetBlinds();

        // SB = (2+1)%3 = 0 → Alice
        // BB = (2+2)%3 = 1 → Bob
        Assert(smallBlindIndex == 0, "SB should wrap to index 0 (Alice)");
        Assert(bigBlindIndex == 1, "BB should be index 1 (Bob)");

        Console.WriteLine("✔️  PASS: Wrap-around SB/BB assignment works.\n");
    }

    // === Helper Methods ===
    static void ResetState()
    {
        players.Clear();
        pot = 0;
        currentBet = 0;
        dealerPosition = 0;
        smallBlindIndex = -1;
        bigBlindIndex = -1;
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