using System;
using System.Collections.Generic;
using System.Net;

// Test script for server.cs's CheckIfAllBetsMatched()
public class TestCheckIfAllBetsMatched
{
    // === Copy of your server state (must match exactly) ===
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

        public override string ToString()
        {
            return $"{Name}(ID:{ID}) Bet={CurrentBet}, Active={IsActive}";
        }
    }

    // === Server state variables (static, as in your server.cs) ===
    private static List<Player> players = new List<Player>();
    private static int currentBet = 0;
    private static bool allBetsMatched = false;
    private static GameState currentGameState = GameState.WaitingForPlayers;
    private static bool isRunning = true;

    // === Function under test (copy-paste from your server.cs) ===
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

    // === Test Runner ===
    static void Main(string[] args)
    {
        Console.WriteLine("=== Unit Testing: CheckIfAllBetsMatched() ===\n");

        // Create dummy endpoint for players
        IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);

        TestAllActivePlayersMatched();
        TestOnePlayerHasNotMatched();
        TestFoldedPlayerIsIgnored();
        TestAllInPlayerBelowCurrentBet();
        TestNoActivePlayersRemaining();
        TestMultiplePlayersWithMixedBets();

        Console.WriteLine("\n✅ All tests passed!");
    }

    // --- Test Case 1: All active players have matched the current bet ---
    static void TestAllActivePlayersMatched()
    {
        Console.WriteLine("🧪 Test 1: All active players matched the bet");
        ResetTestState();

        currentBet = 40;
        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 40 });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 40 });
        players.Add(new Player("Charlie", 3, dummyEP) { CurrentBet = 40 });

        CheckIfAllBetsMatched();

        Assert(allBetsMatched, "Expected allBetsMatched = true when all active players matched");
        Console.WriteLine("✅ Test 1 passed.\n");
    }

    // --- Test Case 2: One player hasn't matched ---
    static void TestOnePlayerHasNotMatched()
    {
        Console.WriteLine("🧪 Test 2: One player hasn't matched current bet");
        ResetTestState();

        currentBet = 50;
        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 50 });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 30 }); // needs 20 more
        players.Add(new Player("Charlie", 3, dummyEP) { CurrentBet = 50 });

        CheckIfAllBetsMatched();

        Assert(!allBetsMatched, "Expected allBetsMatched = false when one player hasn't matched");
        Console.WriteLine("✅ Test 2 passed.\n");
    }

    // --- Test Case 3: Folded player is ignored ---
    static void TestFoldedPlayerIsIgnored()
    {
        Console.WriteLine("🧪 Test 3: Folded player should not block betting round");
        ResetTestState();

        currentBet = 60;
        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 60 });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 60 });
        players.Add(new Player("Charlie", 3, dummyEP) { CurrentBet = 40, IsActive = false }); // folded

        CheckIfAllBetsMatched();

        Assert(allBetsMatched, "Expected true — folded player is inactive and should be ignored");
        Console.WriteLine("✅ Test 3 passed.\n");
    }

    // --- Test Case 4: All-in player hasn't matched full bet ---
    static void TestAllInPlayerBelowCurrentBet()
    {
        Console.WriteLine("🧪 Test 4: All-in player bet less than currentBet");
        ResetTestState();

        currentBet = 70;
        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 70 });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 50 }); // all-in, but < 70
        players.Add(new Player("Charlie", 3, dummyEP) { CurrentBet = 70 });

        CheckIfAllBetsMatched();

        Assert(!allBetsMatched, "Expected false — Bob hasn't matched 70");
        Console.WriteLine("✅ Test 4 passed.\n");
    }

    // --- Test Case 5: No active players (all folded) ---
    static void TestNoActivePlayersRemaining()
    {
        Console.WriteLine("🧪 Test 5: No active players");
        ResetTestState();

        currentBet = 50;
        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 50, IsActive = false });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 50, IsActive = false });

        CheckIfAllBetsMatched();

        Assert(allBetsMatched, "Expected true — no active players means betting round is complete");
        Console.WriteLine("✅ Test 5 passed.\n");
    }

    // --- Test Case 6: Mixed bets with multiple unmatched ---
    static void TestMultiplePlayersWithMixedBets()
    {
        Console.WriteLine("🧪 Test 6: Multiple players with unmatched bets");
        ResetTestState();

        currentBet = 30;
        players.Add(new Player("Alice", 1, dummyEP) { CurrentBet = 30 });
        players.Add(new Player("Bob", 2, dummyEP) { CurrentBet = 20 });
        players.Add(new Player("Charlie", 3, dummyEP) { CurrentBet = 10 });
        players.Add(new Player("Diana", 4, dummyEP) { CurrentBet = 30 });

        CheckIfAllBetsMatched();

        Assert(!allBetsMatched, "Expected false — Bob and Charlie haven't matched");
        Console.WriteLine("✅ Test 6 passed.\n");
    }

    // === Helper Methods ===

    private static IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 8888);

    static void ResetTestState()
    {
        players.Clear();
        currentBet = 0;
        allBetsMatched = false;
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Console.Error.WriteLine($"❌ FAILED: {message}");
            Environment.Exit(1);
        }
    }
}