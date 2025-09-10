# accounts/management/commands/populate_players.py
from django.core.management.base import BaseCommand
from django.contrib.auth.models import User
from accounts.models import Account
import random
from datetime import timedelta

class Command(BaseCommand):
    help = 'Populates the database with 100 random players for testing (safe to re-run)'

    def handle(self, *args, **options):
        # Sample data
        first_names = [
            'Alice', 'Bob', 'Charlie', 'Diana', 'Evan', 'Fiona', 'George', 'Hannah',
            'Ian', 'Julia', 'Kevin', 'Lena', 'Mike', 'Nina', 'Oscar', 'Pamela',
            'Quinn', 'Ryan', 'Sara', 'Tom', 'Uma', 'Victor', 'Wendy', 'Xander',
            'Yara', 'Zack', 'Liam', 'Emma', 'Noah', 'Olivia'
        ]

        last_names = [
            'Smith', 'Johnson', 'Williams', 'Jones', 'Brown', 'Davis', 'Miller',
            'Wilson', 'Moore', 'Taylor', 'Anderson', 'Thomas', 'Jackson', 'White',
            'Harris', 'Martin', 'Thompson', 'Garcia', 'Martinez', 'Robinson',
            'Clark', 'Rodriguez', 'Lewis', 'Lee', 'Walker', 'Hall', 'Allen', 'Young'
        ]

        arenas = [
            'Junkyard',
            'Texas Hold\'em Lounge',
            'Omaha Pit',
            'High Roller Club',
            'Bluff Masters Arena',
            'Cash Game Corner',
            'All-In Arena',
            'Poker Legends Hall',
            'Beginner\'s Table',
            'Final Table Showdown',
            'Underground Poker Den'
        ]

        balances = [500, 1000, 2500, 5000, 10000]

        total_created = 0
        total_updated = 0

        for i in range(100):
            # Generate random data
            fname = random.choice(first_names)
            lname = random.choice(last_names)
            username = f"{fname.lower()}{random.randint(100, 999)}"
            email = f"{username}@example.com"
            name = f"{fname} {lname}"
            arena = random.choice(arenas)
            balance = random.choice(balances)

            # Game stats
            total_games = random.randint(0, 500)
            wins = random.randint(0, total_games)
            losses = total_games - wins
            draws = random.randint(0, max(5, total_games // 20))  # Rare

            # Playtime: up to 200 hours
            hours = random.randint(0, 200)
            minutes = random.randint(0, 59)
            total_playtime = timedelta(hours=hours, minutes=minutes)

            # 👉 Step 1: Get or create User (safe)
            user, user_created = User.objects.get_or_create(
                username=username,
                defaults={
                    'email': email,
                    'first_name': fname,
                    'last_name': lname,
                }
            )

            # 👉 Step 2: Get or create Account (AVOIDS duplicate key error)
            account, created = Account.objects.get_or_create(
                user=user,
                defaults={
                    'name': name,
                    'arena': arena,
                    'balance': balance,
                    'total_games': total_games,
                    'wins': wins,
                    'losses': losses,
                    'draws': draws,
                    'total_playtime': total_playtime,
                }
            )

            if created:
                total_created += 1
            else:
                # 👉 Update existing account with new random data
                account.name = name
                account.arena = arena
                account.balance = balance
                account.total_games = total_games
                account.wins = wins
                account.losses = losses
                account.draws = draws
                account.total_playtime = total_playtime
                account.save()
                total_updated += 1

        self.stdout.write(
            self.style.SUCCESS(
                f'Successfully processed 100 players:\n'
                f'  ✅ {total_created} new accounts created\n'
                f'  🔄 {total_updated} existing accounts updated\n'
                f'  💡 Safe to run again!'
            )
        )