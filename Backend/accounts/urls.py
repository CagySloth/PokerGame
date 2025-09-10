from django.urls import path
from . import views

urlpatterns = [
    path('accounts/', views.account_list, name='account-list'),
    path('leaderboard/games/', views.top_players_by_games, name='top-players-by-games'),
]