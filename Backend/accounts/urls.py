from django.urls import path
from . import views

urlpatterns = [
    path('players/', views.player_list, name='player-list'),
]