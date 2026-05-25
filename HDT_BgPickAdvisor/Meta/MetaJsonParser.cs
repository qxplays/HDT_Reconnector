using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using HDT_BgPickAdvisor.Models;

namespace HDT_BgPickAdvisor.Meta
{
    /// <summary>BG meta JSON from BgMetaApi (hero_dbf_id / trinket_dbf_id rows).</summary>
    public static class MetaJsonParser
    {
        public static List<HeroMeta> ParseHeroes(string json)
        {
            var rank = 1;
            return ParseRows(json)
                .Where(r => r.DbfId > 0 && (r.AvgPlacement.HasValue || r.PickRate.HasValue))
                .OrderByDescending(r => r.AvgPlacement.HasValue)
                .ThenBy(r => r.AvgPlacement ?? double.MaxValue)
                .ThenByDescending(r => r.PickRate ?? 0)
                .Select(r => new HeroMeta
                {
                    DbfId = r.DbfId,
                    AvgPlacement = r.AvgPlacement,
                    PickRate = r.PickRate,
                    Tier = FormatTier(r.Tier, rank),
                    Rank = rank++
                })
                .ToList();
        }

        public static void ParseTrinketsSplit(string json, out List<TrinketMeta> lesser, out List<TrinketMeta> greater)
        {
            var rows = ParseRows(json)
                .Where(r => r.DbfId > 0 && r.AvgPlacement.HasValue)
                .ToList();

            var hasGroup = rows.Any(r => r.Pool != TrinketPool.Unknown);
            if (!hasGroup)
            {
                lesser = BuildTrinketList(rows, TrinketPool.Lesser);
                greater = new List<TrinketMeta>();
                return;
            }

            lesser = BuildTrinketList(rows.Where(r => r.Pool == TrinketPool.Lesser), TrinketPool.Lesser);
            greater = BuildTrinketList(rows.Where(r => r.Pool == TrinketPool.Greater), TrinketPool.Greater);
        }

        private static List<TrinketMeta> BuildTrinketList(IEnumerable<MetaRow> rows, TrinketPool defaultPool)
        {
            var bestPerDbf = rows
                .GroupBy(r => r.DbfId)
                .Select(g => g.OrderBy(r => r.AvgPlacement).First());

            var rank = 1;
            return bestPerDbf
                .OrderBy(r => r.AvgPlacement ?? double.MaxValue)
                .Select(r => new TrinketMeta
                {
                    DbfId = r.DbfId,
                    AvgPlacement = r.AvgPlacement,
                    PickRate = r.PickRate,
                    Tier = FormatTier(r.Tier, rank),
                    Rank = rank++,
                    Pool = r.Pool != TrinketPool.Unknown ? r.Pool : defaultPool
                })
                .ToList();
        }

        internal static List<MetaRow> ParseRows(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart().StartsWith("<", StringComparison.Ordinal))
                return new List<MetaRow>();

            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var root = serializer.DeserializeObject(json);
                if (root is object[] objArray)
                    root = new ArrayList(objArray);

                var items = ExtractItemDictionaries(root);
                var rows = items.Select(MapRow).Where(r => r.DbfId > 0).ToList();
                if (rows.Count > 0)
                    return rows;
            }
            catch
            {
                // ignored
            }

            return new List<MetaRow>();
        }

        private static IEnumerable<Dictionary<string, object>> ExtractItemDictionaries(object root)
        {
            if (root is ArrayList array)
            {
                foreach (var item in array)
                {
                    if (item is Dictionary<string, object> dict)
                        yield return dict;
                }
                yield break;
            }

            if (root is Dictionary<string, object> obj)
            {
                foreach (var key in new[] { "data", "results", "heroes", "trinkets", "items" })
                {
                    if (!obj.TryGetValue(key, out var nested))
                        continue;

                    foreach (var dict in ExtractItemDictionaries(nested))
                        yield return dict;
                }
            }
        }

        private static MetaRow MapRow(Dictionary<string, object> dict) =>
            new MetaRow
            {
                DbfId = ReadInt(dict, "hero_dbf_id", "trinket_dbf_id"),
                AvgPlacement = ReadDouble(dict,
                    "avg_final_placement",
                    "avg_placement",
                    "adjusted_avg_final_placement"),
                PickRate = ReadDouble(dict, "pick_rate", "pickRate"),
                Tier = ReadTier(dict),
                Pool = ParsePool(ReadString(dict, "group"))
            };

        private static TrinketPool ParsePool(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return TrinketPool.Unknown;
            if (group.Equals("greater", StringComparison.OrdinalIgnoreCase))
                return TrinketPool.Greater;
            if (group.Equals("lesser", StringComparison.OrdinalIgnoreCase))
                return TrinketPool.Lesser;
            return TrinketPool.Unknown;
        }

        private static string ReadTier(Dictionary<string, object> dict)
        {
            var v2 = ReadString(dict, "tier_v2");
            if (!string.IsNullOrEmpty(v2))
                return NormalizeTier(v2);
            return NormalizeTier(ReadString(dict, "tier_label", "tier"));
        }

        private static string NormalizeTier(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;
            tier = tier.Trim();
            return tier.Length == 1 ? tier.ToUpperInvariant() : char.ToUpper(tier[0]) + tier.Substring(1);
        }

        private static string FormatTier(string tier, int rank)
        {
            if (!string.IsNullOrWhiteSpace(tier))
                return tier;
            return MetaDefaults.DefaultTier;
        }

        private static int ReadInt(Dictionary<string, object> dict, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!dict.TryGetValue(key, out var value) || value == null)
                    continue;
                if (value is int i) return i;
                if (int.TryParse(value.ToString(), out var parsed)) return parsed;
            }
            return 0;
        }

        private static double? ReadDouble(Dictionary<string, object> dict, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!dict.TryGetValue(key, out var value) || value == null)
                    continue;
                if (value is double d) return d;
                if (value is decimal dec) return (double)dec;
                if (double.TryParse(value.ToString(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
            return null;
        }

        private static string ReadString(Dictionary<string, object> dict, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!dict.TryGetValue(key, out var value) || value == null)
                    continue;
                var s = value.ToString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
            return null;
        }
    }

    internal sealed class MetaRow
    {
        public int DbfId { get; set; }
        public double? AvgPlacement { get; set; }
        public double? PickRate { get; set; }
        public string Tier { get; set; }
        public TrinketPool Pool { get; set; }
    }
}
