from django.urls import path
from . import views

urlpatterns = [
    path('players/', views.player_list, name='player-list'),
    path('leaderboard/games/', views.top_players_by_games, name='top-players-by-games'),
]