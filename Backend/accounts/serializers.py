from rest_framework import serializers
from django.contrib.auth.models import User
from .models import Player

class PlayerSerializer(serializers.ModelSerializer):
    username = serializers.CharField(source='user.username', read_only=True)
    email = serializers.EmailField(source='user.email', read_only=True)

    class Meta:
        model = Player
        fields = [
            'id',
            'name',           # ← New
            'username',
            'email',
            'arena',          # ← New
            'balance',
            'total_games',
            'wins',
            'losses',
            'draws',
            'win_rate',
            'avatar',
            'is_online',
            'last_seen',
            'created_at',
            'total_playtime',
        ]

class PlayerLeaderboardSerializer(serializers.ModelSerializer):
    class Meta:
        model = Player
        fields = ['name', 'arena', 'total_games', 'total_playtime']