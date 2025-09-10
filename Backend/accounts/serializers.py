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
            'username',
            'email',
            'balance',
            'total_games',
            'wins',
            'losses',
            'draws',
            'win_rate',
            'avatar',
            'is_online',
            'last_seen',
            'total_playtime',
            'created_at',
        ]