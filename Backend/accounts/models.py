from django.db import models
from django.contrib.auth.models import User
from django.db.models.signals import post_save
from django.dispatch import receiver


class Account(models.Model):
    id = models.AutoField(primary_key=True)
    user = models.OneToOneField(User, on_delete=models.CASCADE)
    name = models.CharField(max_length=150, blank=False, null=False)  # Display name
    arena = models.CharField(max_length=100, default="Junkyard")  # Arena name
    balance = models.DecimalField(max_digits=12, decimal_places=2, default=1000.00) # Starting balance
    total_games = models.PositiveIntegerField(default=0) # Total number of hands
    wins = models.PositiveIntegerField(default=0)
    losses = models.PositiveIntegerField(default=0)
    draws = models.PositiveIntegerField(default=0)
    win_rate = models.FloatField(default=0.0)  # Automatically updated
    avatar = models.URLField(blank=True, null=True)  # Or use ImageField if storing locally
    is_online = models.BooleanField(default=False)
    last_seen = models.DateTimeField(auto_now=True)
    total_playtime = models.DurationField(default=0)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return self.user.username

    def save(self, *args, **kwargs):
        # Auto-calculate win rate
        if self.total_games > 0:
            self.win_rate = round((self.wins / self.total_games) * 100, 2)
        else:
            self.win_rate = 0.0
        super().save(*args, **kwargs)


@receiver(post_save, sender=User)
def create_player_profile(sender, instance, created, **kwargs):
    if created:
        full_name = instance.get_full_name().strip()
        name = full_name if full_name else instance.username
        Account.objects.create(
            user=instance,
            name=name,
            arena="General Arena"  # Default arena
        )

@receiver(post_save, sender=User)
def save_player_profile(sender, instance, **kwargs):
    """Save the Player profile when the User is saved."""
    instance.player.save()