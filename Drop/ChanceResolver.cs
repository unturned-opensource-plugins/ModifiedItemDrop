using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public sealed class ChanceResolver
    {
        private DropRuleSet _ruleSet;
        private readonly Dictionary<string, double> _regionOverrides = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ushort, double> _itemOverrides = new Dictionary<ushort, double>();

        public ChanceResolver(DropRuleSet ruleSet)
        {
            UpdateRuleSet(ruleSet);
        }

        public void UpdateRuleSet(DropRuleSet ruleSet)
        {
            _ruleSet = ruleSet ?? DropRuleSet.CreateDefault();
        }

        public IReadOnlyDictionary<string, double> RegionOverrides => _regionOverrides;

        public IReadOnlyDictionary<ushort, double> ItemOverrides => _itemOverrides;

        public double GetChance(SlotType slotType, ushort itemId, out string source)
        {
            if (_itemOverrides.TryGetValue(itemId, out var itemOverride))
            {
                source = $"Override:Item:{itemId}";
                return itemOverride;
            }

            var slotKey = slotType.ToString();
            if (_regionOverrides.TryGetValue(slotKey, out var regionOverride))
            {
                source = $"Override:Region:{slotKey}";
                return regionOverride;
            }

            if (_ruleSet == null)
            {
                source = "Global";
                return 0d;
            }

            return _ruleSet.ResolveChance(slotType, itemId, out source);
        }

        public void SetRegionOverride(string region, double chance)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return;
            }

            _regionOverrides[region.Trim()] = UtilityHelper.ClampChance(chance);
        }

        public void SetItemOverride(ushort itemId, double chance)
        {
            _itemOverrides[itemId] = UtilityHelper.ClampChance(chance);
        }

        public bool ClearRegionOverride(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return false;
            }

            return _regionOverrides.Remove(region.Trim());
        }

        public bool ClearItemOverride(ushort itemId)
        {
            return _itemOverrides.Remove(itemId);
        }

        public void ClearAllOverrides()
        {
            _regionOverrides.Clear();
            _itemOverrides.Clear();
        }

        public void ClearAllRegionOverrides()
        {
            _regionOverrides.Clear();
        }

        public void ClearAllItemOverrides()
        {
            _itemOverrides.Clear();
        }
    }
}

