from rest_framework.decorators import api_view
from rest_framework.response import Response
from .models import Player
from .serializers import PlayerSerializer, PlayerLeaderboardSerializer

@api_view(['GET'])
def player_list(request):
    """
    Return a list of all players with info.
    """
    players = Player.objects.all().select_related('user')  # Efficient query
    serializer = PlayerSerializer(players, many=True)
    return Response(serializer.data)

@api_view(['GET'])
def top_players_by_games(request):
    """
    Return top 50 players with the highest total_games.
    """
    players = Player.objects.order_by('-total_games')[:50]
    serializer = PlayerLeaderboardSerializer(players, many=True)
    return Response(serializer.data)