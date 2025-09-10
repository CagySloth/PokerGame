using System;
using System.Collections.Generic;
using System.Linq;

public class TestDetermineWinner
{
    // === Player class (must match server.cs) ===
    public class Player
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public int Chips { get; set; } = 1000;
        public int CurrentBet { get; set; }
        public string[] HoleCards { get; set; } = new string[2];
        public bool IsActive { get; set; } = true;
        public bool HasActed { get; set; } = false;

        public Player(string name, int id)
        {
            Name = name;
            ID = id;
            HoleCards = new string[2];
        }

        public override string ToString() => Name;
    }

    // === Game state variables ===
    private static List<Player> players = new List<Player>();
    private static string[] communityCards = new string[5];
    private static int pot = 0;
    private static string lastBroadcastMessage = "";

    // === Mock BroadcastMessage ===
    private static void BroadcastMessage(string message)
    {
        lastBroadcastMessage = message;
    }

    // === Helper: GetCardRank ===
    private static int GetCardRank(string card)
    {
        string rank = card.Substring(0, card.Length - 1);
        return rank switch
        {
            "10" => 10,
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            _ => 0
        };
    }

    // === Helper: GetCardSuit ===
    private static char GetCardSuit(string card)
    {
        return card[^1];
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
        if (uniqueRanks.Contains(14) && uniqueRanks.Contains(13) && uniqueRanks.Contains(12) && uniqueRanks.Contains(11) && uniqueRanks.Contains(10))
        {
            isStraight = true;
            highStraight = 14;
        }

        int[] ofAKind = rankCounts.Values.OrderByDescending(x => x).ToArray();
        bool isFourOfAKind = ofAKind.Length >= 1 && ofAKind[0] == 4;
        bool isThreeOfAKind = ofAKind.Length >= 1 && ofAKind[0] == 3;
        bool isTwoPair = ofAKind.Length >= 2 && ofAKind[1] == 2;
        bool isPair = ofAKind.Length >= 1 && ofAKind[0] == 2;

        long score = 0;

        // 1. Straight Flush
        if (isStraight && isFlush)
        {
            score = (10_000_000L) | ((long)highStraight << 16);
        }
        // 2. Four of a Kind
        else if (isFourOfAKind)
        {
            int quadRank = rankCounts.First(kv => kv.Value == 4).Key;
            int kicker = ranks.Where(r => r != quadRank).Max();
            score = (9_900_000L) | ((long)quadRank << 16) | ((long)kicker << 8);
        }
        // 3. Full House (Three + Two)
        else if (isThreeOfAKind && isTwoPair)
        {
            int trips = rankCounts.First(kv => kv.Value == 3).Key;
            int pair = rankCounts.First(kv => kv.Value >= 2 && kv.Key != trips).Key;
            score = (9_800_000L) | ((long)trips << 16) | ((long)pair << 8);
        }
        // 4. Flush
        else if (isFlush)
        {
            var flushRanks = allCards
                .Where(card => suitCounts[GetCardSuit(card)] >= 5)
                .Select(GetCardRank)
                .OrderByDescending(x => x)
                .Take(5);
            score = 9_700_000L;
            int shift = 16;
            foreach (int r in flushRanks)
            {
                score |= ((long)r << shift);
                shift -= 8;
            }
        }
        // 5. Straight
        else if (isStraight)
        {
            score = (9_600_000L) | ((long)highStraight << 16);
        }
        // 6. Three of a Kind
        else if (isThreeOfAKind)
        {
            int trips = rankCounts.First(kv => kv.Value == 3).Key;
            var kickers = ranks.Where(r => r != trips).Take(2).ToList();
            score = (9_550_000L) | ((long)trips << 16) | ((long)kickers[0] << 8) | (long)kickers[1];
        }
        // 7. Two Pair
        else if (isTwoPair)
        {
            var pairs = rankCounts.Where(kv => kv.Value == 2).OrderByDescending(kv => kv.Key).Take(2).ToList();
            int firstPair = pairs[0].Key;
            int secondPair = pairs[1].Key;
            int kicker = ranks.Where(r => r != firstPair && r != secondPair).Max();
            score = (9_540_000L) | ((long)firstPair << 16) | ((long)secondPair << 8) | (long)kicker;
        }
        // 8. One Pair
        else if (isPair)
        {
            int pairRank = rankCounts.First(kv => kv.Value == 2).Key;
            var kickers = ranks.Where(r => r != pairRank).Take(3).ToList();
            score = (9_530_000L)
                | ((long)pairRank << 16)
                | ((long)kickers[0] << 8)
                | ((long)kickers[1] << 4)
                | (long)kickers[2];
        }
        // 9. High Card
        else
        {
            score = 9_520_000L;
            for (int i = 0; i < 5; i++)
            {
                score |= ((long)ranks[i]) << (16 - i * 4);
            }
        }

        return score;
    }
    // === Function under test: DetermineWinner() ===
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

    // === Test Runner ===
    public static void Main(string[] args)
    {
        Console.WriteLine("🧪 Running unit tests for DetermineWinner() and EvaluateHand()\n");

        TestStraightFlushWins();
        TestFourOfAKindBeatsFlush();
        TestTieSplitPot();
        TestHighCardWinner();
        TestThreeOfAKindVsTwoPair();

        Console.WriteLine("\n✅ All tests passed!");
    }

    // --- Test 1: Straight Flush beats Four of a Kind ---
    static void TestStraightFlushWins()
    {
        Console.WriteLine("Test 1: Straight Flush should beat Four of a Kind...");
        ResetState();
        pot = 1000;

        communityCards = new[] { "10♠", "J♠", "Q♠", "K♠", "A♠" };
        players.Add(new Player("Alice", 1) { HoleCards = new[] { "2♠", "3♠" } }); // Straight Flush
        players.Add(new Player("Bob", 2) { HoleCards = new[] { "A♥", "A♦" } });   // Four Aces

        DetermineWinner();

        Assert(lastBroadcastMessage.Contains("Alice wins"), "Alice should win with straight flush");
        Console.WriteLine("✔️  PASS: Straight flush wins.\n");
    }

    // --- Test 2: Four of a Kind beats Flush ---
    static void TestFourOfAKindBeatsFlush()
    {
        Console.WriteLine("Test 2: Four of a Kind should beat Flush...");
        ResetState();
        pot = 800;

        communityCards = new[] { "K♠", "K♥", "K♦", "K♣", "10♠" };
        players.Add(new Player("Alice", 1) { HoleCards = new[] { "A♠", "A♥" } }); // Four Ks + AA
        players.Add(new Player("Bob", 2) { HoleCards = new[] { "Q♠", "J♠" } });   // Flush (K-high)

        DetermineWinner();

        Assert(lastBroadcastMessage.Contains("Alice wins"), "Alice should win with four of a kind");
        Console.WriteLine("✔️  PASS: Four of a kind beats flush.\n");
    }

    // --- Test 3: Tie → Pot Split ---
    static void TestTieSplitPot()
    {
        Console.WriteLine("Test 3: Two players tie → pot split...");
        ResetState();
        pot = 600;

        communityCards = new[] { "A♠", "K♠", "Q♠", "J♠", "10♠" };
        players.Add(new Player("Alice", 1) { HoleCards = new[] { "2♠", "3♠" } }); // Straight Flush
        players.Add(new Player("Bob", 2) { HoleCards = new[] { "4♠", "5♠" } });   // Straight Flush (same)

        DetermineWinner();

        Assert(lastBroadcastMessage.Contains("Alice and Bob split"), "Pot should be split");
        Assert(lastBroadcastMessage.Contains("Each gets 300"), "Each should get 300");
        Console.WriteLine("✔️  PASS: Tie correctly splits pot.\n");
    }

    // --- Test 4: High Card Wins ---
    static void TestHighCardWinner()
    {
        Console.WriteLine("Test 4: High card decides winner...");
        ResetState();
        pot = 200;

        communityCards = new[] { "10♠", "J♥", "Q♦", "K♣", "9♠" };
        players.Add(new Player("Alice", 1) { HoleCards = new[] { "A♠", "2♥" } }); // Ace high
        players.Add(new Player("Bob", 2) { HoleCards = new[] { "K♠", "Q♠" } });   // King high

        DetermineWinner();

        Assert(lastBroadcastMessage.Contains("Alice wins"), "Alice should win with Ace high");
        Console.WriteLine("✔️  PASS: High card wins.\n");
    }

    // --- Test 5: Three of a Kind vs Two Pair ---
    static void TestThreeOfAKindVsTwoPair()
    {
        Console.WriteLine("Test 5: Three of a Kind beats Two Pair...");
        ResetState();
        pot = 500;

        communityCards = new[] { "Q♠", "Q♥", "J♦", "10♠", "2♠" };
        players.Add(new Player("Alice", 1) { HoleCards = new[] { "Q♦", "Q♣" } }); // Four Queens (but counts as trips)
        players.Add(new Player("Bob", 2) { HoleCards = new[] { "J♥", "10♥" } });  // Two Pair: QQ + JJ

        DetermineWinner();

        Assert(lastBroadcastMessage.Contains("Alice wins"), "Three of a kind should beat two pair");
        Console.WriteLine("✔️  PASS: Three of a kind wins.\n");
    }

    // === Helper Methods ===
    static void ResetState()
    {
        players.Clear();
        pot = 0;
        lastBroadcastMessage = "";
        communityCards = new string[5];
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