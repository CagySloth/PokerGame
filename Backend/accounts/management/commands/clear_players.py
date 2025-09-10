# accounts/management/commands/clear_players.py
from django.core.management.base import BaseCommand
from django.contrib.auth.models import User
from accounts.models import Account

class Command(BaseCommand):
    help = '🔥 NUKES all test players and any orphaned accounts — use with caution!'

    def handle(self, *args, **options):
        # Pattern for auto-generated usernames like "alice123"
        test_username_pattern = r'^[a-z]{3,8}[0-9]{3}$'

        # Step 1: Delete ALL Account objects (safe to do first)
        accounts_before = Account.objects.count()
        Account.objects.all().delete()
        self.stdout.write(self.style.WARNING(f"🧹 Deleted {accounts_before} Account objects (all)."))

        # Step 2: Delete all test users
        test_users = User.objects.filter(username__regex=test_username_pattern)
        test_count = test_users.count()
        test_users.delete()
        self.stdout.write(self.style.WARNING(f"🗑️  Deleted {test_count} test User objects."))

        # Step 3: Also delete any User that has no associated account (in case of partial cleanup)
        users_without_account = User.objects.filter(account__isnull=True)
        extra_count = users_without_account.count()
        users_without_account.delete()
        if extra_count > 0:
            self.stdout.write(self.style.WARNING(f"🧹 Cleaned up {extra_count} users without accounts."))

        # Final report
        self.stdout.write(
            self.style.SUCCESS(
                "✅ Database fully cleaned!\n"
                "You can now safely run populate_players."
            )
        )