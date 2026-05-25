using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BgPowerLog.Models;

namespace BgPowerLog.Parsing
{
    /// <summary>
    /// Parses Hearthstone Power.log (GameState.DebugPrintPower lines only).
    /// Does not use HDT — reads Blizzard log file directly.
    /// </summary>
    public sealed class PowerLogParser
    {
        private static readonly Regex PowerLineBody =
            new Regex(@"\.DebugPrintPower\(\)\s*-\s*(.+)$", RegexOptions.Compiled);

        private static readonly Regex UpdatingEntityBracket =
            new Regex(@"Updating\s+\[[^\]]*?\bid=(\d+)[^\]]*?(?:cardId=(\S+))?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CreateEntity =
            new Regex(@"^(?:FULL_ENTITY|SHOW_ENTITY).*- (?:Creating ID=(\d+)|Updating .*?id=(\d+)).*?CardID=(\S+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TagChangeBracket =
            new Regex(@"^TAG_CHANGE Entity=\[[^\]]*?\bid=(\d+)[^\]]*\]\s+tag=(\S+)\s+value=(\S+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PlayerInEntityBracket =
            new Regex(@"\bplayer=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TagChangeGame =
            new Regex(@"^TAG_CHANGE Entity=GameEntity tag=(\S+) value=(\S+)", RegexOptions.Compiled);

        private static readonly Regex IndentedTag =
            new Regex(@"^\s*tag=(\S+)\s+value=(\S+)", RegexOptions.Compiled);

        private readonly Dictionary<int, EntityState> _entities = new Dictionary<int, EntityState>();
        private readonly List<ReplayMatch> _matches = new List<ReplayMatch>();
        private ReplayMatch _active;
        private EntityState _pendingEntity;
        private int _currentTurn;
        private bool _isBattlegrounds;
        private bool _activeIsBattlegrounds;
        private int _friendlyController;
        private int _opponentController;
        private int _localPlayerId;
        private int _opponentPlayerId;
        private int _pendingCombatSnapshotTurn;
        private EntityState _localPlayerHero;
        private EntityState _dummyPlayerHero;

        public ReplayParseResult ParseFile(string path, long maxBytes = 64 * 1024 * 1024)
        {
            var result = new ReplayParseResult { SourcePath = path };
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                result.Error = "Power.log not found: " + path;
                return result;
            }

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length > maxBytes)
                        stream.Seek(-maxBytes, SeekOrigin.End);

                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            result.LinesRead++;
                            ProcessLine(line, result);
                        }
                    }
                }

                FinalizeAll(result);
                result.Success = result.Matches.Count > 0 && result.Match.Turns.Count > 0;
                result.IsBattlegrounds = _isBattlegrounds;
                result.EntitiesTracked = _entities.Count;
                result.MatchCount = result.Matches.Count;
                if (!result.Success && string.IsNullOrEmpty(result.Error))
                    result.Error = "No GameState power lines or turns parsed.";
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        public ReplayParseResult ParseTail(string path, int maxLines = 15000)
        {
            var result = new ReplayParseResult { SourcePath = path };
            if (!File.Exists(path))
            {
                result.Error = "Power.log not found: " + path;
                return result;
            }

            try
            {
                var lines = ReadTailLines(path, maxLines);
                foreach (var line in lines)
                {
                    result.LinesRead++;
                    ProcessLine(line, result);
                }

                FinalizeAll(result);
                result.Success = result.Match.Turns.Count > 0;
                result.IsBattlegrounds = _isBattlegrounds;
                result.EntitiesTracked = _entities.Count;
                result.MatchCount = result.Matches.Count;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        private void ProcessLine(string line, ReplayParseResult result)
        {
            if (string.IsNullOrWhiteSpace(line) || line.IndexOf("DebugPrintPower()", StringComparison.Ordinal) < 0)
                return;

            if (line.IndexOf("GameState.DebugPrintPower()", StringComparison.Ordinal) < 0 &&
                line.IndexOf("PowerTaskList.DebugPrintPower()", StringComparison.Ordinal) < 0)
                return;

            var m = PowerLineBody.Match(line);
            if (!m.Success)
                return;

            if (line.IndexOf("GameState.DebugPrintPower()", StringComparison.Ordinal) >= 0)
                result.GameStateLines++;
            EnsureActiveMatch();

            var body = m.Groups[1].Value;
            var bodyTrim = body.TrimStart();

            if (body.IndexOf("BACON", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _isBattlegrounds = true;
                _activeIsBattlegrounds = true;
            }

            if (bodyTrim.StartsWith("CREATE_GAME", StringComparison.OrdinalIgnoreCase))
            {
                StartNewMatch();
                return;
            }

            var updating = UpdatingEntityBracket.Match(bodyTrim);
            if (updating.Success)
            {
                var id = int.Parse(updating.Groups[1].Value);
                var entity = GetOrCreate(id);
                if (updating.Groups[2].Success && !string.IsNullOrEmpty(updating.Groups[2].Value))
                    entity.CardId = updating.Groups[2].Value;
                ApplyBracketFields(bodyTrim, entity);
            }

            var create = CreateEntity.Match(bodyTrim);
            if (create.Success)
            {
                var id = int.Parse(create.Groups[1].Success ? create.Groups[1].Value : create.Groups[2].Value);
                var cardId = create.Groups[3].Value;
                _pendingEntity = GetOrCreate(id);
                _pendingEntity.CardId = cardId;
                ApplyBracketFields(bodyTrim, _pendingEntity);
                if (cardId.IndexOf("BACON", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    cardId.StartsWith("BG", StringComparison.OrdinalIgnoreCase) ||
                    cardId.StartsWith("TB_Bacon", StringComparison.OrdinalIgnoreCase))
                    _isBattlegrounds = true;
                return;
            }

            var tagIndent = IndentedTag.Match(body);
            if (!tagIndent.Success)
                tagIndent = IndentedTag.Match(bodyTrim);
            if (tagIndent.Success && _pendingEntity != null)
            {
                ApplyTag(_pendingEntity, tagIndent.Groups[1].Value, tagIndent.Groups[2].Value);
                return;
            }

            _pendingEntity = null;

            var tagGame = TagChangeGame.Match(bodyTrim);
            if (tagGame.Success)
            {
                ApplyGameTag(tagGame.Groups[1].Value, tagGame.Groups[2].Value, result);
                return;
            }

            var tagEnt = TagChangeBracket.Match(bodyTrim);
            if (tagEnt.Success)
            {
                var id = int.Parse(tagEnt.Groups[1].Value);
                var entity = GetOrCreate(id);
                ApplyBracketFields(bodyTrim, entity);
                ApplyTag(entity, tagEnt.Groups[2].Value, tagEnt.Groups[3].Value);
            }
        }

        private static readonly Regex ZoneInEntityBracket =
            new Regex(@"\bzone=(\S+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CardIdInEntityBracket =
            new Regex(@"\bcardId=(\S+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static void ApplyBracketFields(string body, EntityState entity)
        {
            var pm = PlayerInEntityBracket.Match(body);
            if (pm.Success && int.TryParse(pm.Groups[1].Value, out var player))
                entity.PlayerId = player;

            var zm = ZoneInEntityBracket.Match(body);
            if (zm.Success)
                entity.Zone = zm.Groups[1].Value;

            var cm = CardIdInEntityBracket.Match(body);
            if (cm.Success && !string.IsNullOrEmpty(cm.Groups[1].Value))
                entity.CardId = cm.Groups[1].Value;
        }

        private void EnsureActiveMatch()
        {
            if (_active == null)
                StartNewMatch();
        }

        private void ApplyGameTag(string tag, string value, ReplayParseResult result)
        {
            EnsureActiveMatch();

            if (tag == "TURN" && int.TryParse(value, out var turn) && turn != _currentTurn)
            {
                _currentTurn = turn;
                if (turn % 2 == 0)
                    _pendingCombatSnapshotTurn = turn;
                else
                {
                    _pendingCombatSnapshotTurn = 0;
                    SnapshotTurn(_active, turn, "Recruit");
                }
            }
            else if (tag == "BOARD_VISUAL_STATE" && value == "2" && _currentTurn > 0 && _currentTurn % 2 == 0)
            {
                _pendingCombatSnapshotTurn = 0;
                SnapshotTurn(_active, _currentTurn, "Combat");
            }
            else if (tag == "STEP" && value == "MAIN_READY" && _pendingCombatSnapshotTurn > 0)
            {
                _pendingCombatSnapshotTurn = 0;
                SnapshotTurn(_active, _currentTurn, "Combat");
            }
        }

        private void ApplyTag(EntityState e, string tag, string value)
        {
            switch (tag)
            {
                case "ZONE":
                    e.Zone = value;
                    break;
                case "CARDTYPE":
                    e.CardType = value;
                    break;
                case "CONTROLLER":
                    if (int.TryParse(value, out var ctrl))
                    {
                        e.Controller = ctrl;
                        ResolveControllers(e);
                    }
                    break;
                case "PLAYER_ID":
                case "1037":
                    if (int.TryParse(value, out var pid))
                    {
                        e.PlayerId = pid;
                        ResolveControllers(e);
                    }
                    break;
                case "ZONE_POSITION":
                    int.TryParse(value, out var zp);
                    e.ZonePosition = zp;
                    break;
                case "ATK":
                    int.TryParse(value, out var atk);
                    e.Attack = atk;
                    break;
                case "HEALTH":
                    int.TryParse(value, out var hp);
                    e.Health = hp;
                    break;
                case "DAMAGE":
                    int.TryParse(value, out var dmg);
                    e.Damage = dmg;
                    break;
                case "TECH_LEVEL":
                case "PLAYER_TECH_LEVEL":
                    int.TryParse(value, out var tl);
                    e.TechLevel = tl;
                    break;
                case "PREMIUM":
                    int.TryParse(value, out var prem);
                    e.Premium = prem;
                    break;
                case "TAUNT":
                    int.TryParse(value, out var taunt);
                    e.Taunt = taunt;
                    break;
                case "DIVINE_SHIELD":
                    int.TryParse(value, out var ds);
                    e.DivineShield = ds;
                    break;
                case "POISONOUS":
                    int.TryParse(value, out var pois);
                    e.Poisonous = pois;
                    break;
                case "BACON_DUMMY_PLAYER":
                    int.TryParse(value, out var dummy);
                    e.BaconDummyPlayer = dummy;
                    if (dummy > 0)
                        _opponentController = e.Controller > 0 ? e.Controller : e.PlayerId;
                    break;
            }

            if (!string.IsNullOrEmpty(e.CardId) &&
                (e.CardId.IndexOf("BACON", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 e.CardId.StartsWith("BG", StringComparison.OrdinalIgnoreCase)))
                _isBattlegrounds = true;
        }

        private void ResolveControllers(EntityState e)
        {
            var owner = EntityOwner(e);

            if (e.BaconDummyPlayer > 0)
            {
                _opponentController = owner;
                if (owner > 0)
                    _opponentPlayerId = owner;
                if (e.IsHero && e.Zone == "PLAY")
                    _dummyPlayerHero = e;
                return;
            }

            if (e.IsHero && e.Zone == "PLAY" && e.BaconDummyPlayer == 0)
            {
                _localPlayerHero = e;
                _friendlyController = owner;
                if (owner > 0)
                    _localPlayerId = owner;
            }
            else if (e.CardType == "PLAYER")
            {
                if (e.BaconDummyPlayer > 0)
                {
                    _opponentController = owner;
                    if (owner > 0)
                        _opponentPlayerId = owner;
                }
                else if (_friendlyController == 0)
                {
                    _friendlyController = owner;
                    if (owner > 0)
                        _localPlayerId = owner;
                }
            }
        }

        private static int EntityOwner(EntityState e)
        {
            if (e.PlayerId > 0)
                return e.PlayerId;
            return e.Controller > 0 ? e.Controller : 0;
        }

        private EntityState GetFriendlyReferenceHero()
        {
            if (_localPlayerHero != null)
                return _localPlayerHero;

            return _entities.Values
                .FirstOrDefault(x => x.IsHero && x.Zone == "PLAY" && x.BaconDummyPlayer == 0);
        }

        private EntityState GetOpponentReferenceHero()
        {
            if (_dummyPlayerHero != null)
                return _dummyPlayerHero;

            return _entities.Values
                .FirstOrDefault(x => x.IsHero && x.Zone == "PLAY" && x.BaconDummyPlayer > 0);
        }

        private bool IsOpponentSide(EntityState e)
        {
            var owner = EntityOwner(e);
            if (owner == 0)
                return false;

            if (_localPlayerId > 0)
                return owner != _localPlayerId;

            if (_opponentPlayerId > 0)
                return owner == _opponentPlayerId;

            if (_friendlyController > 0 && _opponentController > 0)
                return owner == _opponentController;

            return false;
        }

        private void SnapshotTurn(ReplayMatch match, int turn, string phase)
        {
            if (match == null || turn <= 0)
                return;

            if (match.Turns.Count > 0 && match.Turns[match.Turns.Count - 1].TurnNumber == turn)
            {
                var last = match.Turns[match.Turns.Count - 1];
                last.Phase = phase;
                last.Friendly = BuildBoard(true, phase);
                last.Opponent = BuildBoard(false, phase);
                return;
            }

            match.Turns.Add(new ReplayTurn
            {
                TurnNumber = turn,
                Phase = phase,
                Friendly = BuildBoard(true, phase),
                Opponent = BuildBoard(false, phase)
            });
        }

        private ReplayBoard BuildBoard(bool friendly, string phase = null)
        {
            var board = new ReplayBoard();
            var combatPhase = string.Equals(phase, "Combat", StringComparison.OrdinalIgnoreCase);

            if (!combatPhase && !friendly)
                return board;

            foreach (var e in _entities.Values)
            {
                var isOpp = IsOpponentSide(e);
                if (friendly && isOpp)
                    continue;
                if (!friendly && !isOpp)
                    continue;

                if (e.IsHero)
                {
                    if (!IsReplayableHero(e, combatPhase, isOpp))
                        continue;

                    if (board.Hero != null && board.Hero.CardId.StartsWith("BG", StringComparison.OrdinalIgnoreCase))
                        continue;

                    board.Hero = new ReplayHero
                    {
                        CardId = e.CardId,
                        EntityId = e.EntityId,
                        Health = e.EffectiveHealth,
                        TechLevel = e.TechLevel
                    };
                    continue;
                }

                if (!IsBoardZone(e, friendly, combatPhase))
                    continue;

                if (!e.IsMinion || string.IsNullOrEmpty(e.CardId) || !IsReplayableMinion(e))
                    continue;

                board.Minions.Add(new ReplayMinion
                {
                    EntityId = e.EntityId,
                    CardId = e.CardId,
                    Attack = e.Attack,
                    Health = e.EffectiveHealth,
                    ZonePosition = e.ZonePosition,
                    Premium = e.Premium > 0,
                    Taunt = e.Taunt > 0,
                    DivineShield = e.DivineShield > 0,
                    Poisonous = e.Poisonous > 0
                });
            }

            board.Minions.Sort((a, b) => a.ZonePosition.CompareTo(b.ZonePosition));
            return board;
        }

        private bool IsBoardZone(EntityState e, bool friendly, bool combatPhase)
        {
            if (e.Zone == "REMOVEDFROMGAME" || e.Zone == "GRAVEYARD")
                return false;

            var owner = EntityOwner(e);
            if (combatPhase && _localPlayerId > 0)
            {
                if (friendly)
                    return e.Zone == "PLAY" && owner == _localPlayerId;
                return (e.Zone == "SETASIDE" || e.Zone == "PLAY") &&
                       (_opponentPlayerId == 0 || owner == _opponentPlayerId);
            }

            if (e.Zone != "PLAY")
                return false;

            if (_localPlayerId > 0 && friendly)
                return owner == _localPlayerId;
            if (_localPlayerId > 0 && !friendly)
                return owner != _localPlayerId && owner > 0;

            return true;
        }

        private static bool IsReplayableHero(EntityState e, bool combatPhase, bool opponent)
        {
            if (string.IsNullOrEmpty(e.CardId))
                return false;

            if (e.CardId.StartsWith("TB_Bacon", StringComparison.OrdinalIgnoreCase) &&
                !e.CardId.StartsWith("TB_BaconShop_HERO", StringComparison.OrdinalIgnoreCase))
                return false;

            if (e.CardId.IndexOf("HERO", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (combatPhase)
                return e.Zone == "SETASIDE" || e.Zone == "PLAY";

            return e.Zone == "PLAY";
        }

        private static bool IsReplayableMinion(EntityState e)
        {
            if (e.CardType == "ENCHANTMENT" || e.CardType == "SPELL")
                return false;

            var id = e.CardId;
            if (id.IndexOf("_e", StringComparison.Ordinal) >= 0)
                return false;
            if (id.StartsWith("TB_Bacon", StringComparison.OrdinalIgnoreCase))
                return false;
            if (id.StartsWith("BG_ShopBuff", StringComparison.OrdinalIgnoreCase))
                return false;
            if (id.StartsWith("EBG_Spell", StringComparison.OrdinalIgnoreCase))
                return false;

            return id.StartsWith("BG", StringComparison.OrdinalIgnoreCase) ||
                   id.StartsWith("BGS_", StringComparison.OrdinalIgnoreCase);
        }

        private void StartNewMatch()
        {
            CommitActiveMatch();
            _entities.Clear();
            _pendingEntity = null;
            _currentTurn = 0;
            _friendlyController = 0;
            _opponentController = 0;
            _localPlayerId = 0;
            _opponentPlayerId = 0;
            _pendingCombatSnapshotTurn = 0;
            _localPlayerHero = null;
            _dummyPlayerHero = null;
            _activeIsBattlegrounds = false;
            _active = new ReplayMatch { Index = _matches.Count };
        }

        private void CommitActiveMatch()
        {
            if (_active == null)
                return;

            _active.FriendlyPlayerId = _localPlayerId > 0
                ? _localPlayerId
                : GetFriendlyReferenceHero() != null
                    ? EntityOwner(GetFriendlyReferenceHero())
                    : _friendlyController;
            _active.OpponentPlayerId = _opponentPlayerId > 0
                ? _opponentPlayerId
                : GetOpponentReferenceHero() != null
                    ? EntityOwner(GetOpponentReferenceHero())
                    : _opponentController;
            _active.IsBattlegrounds = _activeIsBattlegrounds;

            if (_pendingCombatSnapshotTurn > 0)
                SnapshotTurn(_active, _pendingCombatSnapshotTurn, "Combat");

            if (_active.Turns.Count == 0 && _currentTurn > 0)
                SnapshotTurn(_active, _currentTurn, "Unknown");

            if (_active.Turns.Count > 0 || _activeIsBattlegrounds)
                _matches.Add(_active);

            _active = null;
        }

        private void FinalizeAll(ReplayParseResult result)
        {
            CommitActiveMatch();

            var utc = DateTime.UtcNow;
            for (var i = 0; i < _matches.Count; i++)
            {
                _matches[i].Index = i + 1;
                _matches[i].ParsedUtc = utc;
                _matches[i].SourcePath = result.SourcePath;
            }

            result.Matches = _matches;
            result.Match = _matches.LastOrDefault(m => m.Turns.Count > 0)
                           ?? _matches.LastOrDefault()
                           ?? new ReplayMatch();
            result.MatchCount = _matches.Count;
        }

        private EntityState GetOrCreate(int id)
        {
            if (!_entities.TryGetValue(id, out var e))
            {
                e = new EntityState { EntityId = id };
                _entities[id] = e;
            }

            return e;
        }

        private static List<string> ReadTailLines(string path, int maxLines)
        {
            var lines = new LinkedList<string>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.AddLast(line);
                    if (lines.Count > maxLines)
                        lines.RemoveFirst();
                }
            }

            return new List<string>(lines);
        }
    }
}
