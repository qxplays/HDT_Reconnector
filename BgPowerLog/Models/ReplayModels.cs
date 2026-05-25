using System;
using System.Collections.Generic;

namespace BgPowerLog.Models
{
    public sealed class ReplayParseResult
    {
        public string SourcePath { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public bool IsBattlegrounds { get; set; }
        public int LinesRead { get; set; }
        public int GameStateLines { get; set; }
        public int EntitiesTracked { get; set; }
        public int MatchCount { get; set; }
        public List<ReplayMatch> Matches { get; set; } = new List<ReplayMatch>();
        public ReplayMatch Match { get; set; } = new ReplayMatch();
    }

    public sealed class ReplayMatch
    {
        public int Index { get; set; }
        public string SessionLabel { get; set; }
        public DateTime? ParsedUtc { get; set; }
        public string SourcePath { get; set; }
        public bool IsBattlegrounds { get; set; }
        public int FriendlyPlayerId { get; set; }
        public int OpponentPlayerId { get; set; }
        public List<ReplayTurn> Turns { get; set; } = new List<ReplayTurn>();
    }

    public sealed class ReplayTurn
    {
        public int TurnNumber { get; set; }
        public string Phase { get; set; }
        public ReplayBoard Friendly { get; set; } = new ReplayBoard();
        public ReplayBoard Opponent { get; set; } = new ReplayBoard();
    }

    public sealed class ReplayBoard
    {
        public ReplayHero Hero { get; set; }
        public List<ReplayMinion> Minions { get; set; } = new List<ReplayMinion>();
    }

    public sealed class ReplayHero
    {
        public string CardId { get; set; }
        public int EntityId { get; set; }
        public int Health { get; set; }
        public int TechLevel { get; set; }
    }

    public sealed class ReplayMinion
    {
        public int EntityId { get; set; }
        public string CardId { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int ZonePosition { get; set; }
        public bool Premium { get; set; }
        public bool Taunt { get; set; }
        public bool DivineShield { get; set; }
        public bool Poisonous { get; set; }
    }
}
