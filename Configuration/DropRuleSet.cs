using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class DropRuleSet
    {
        public double GlobalDefaultChance { get; set; } = 0.5d;

        public List<RegionChanceEntry> RegionChances { get; set; } = new List<RegionChanceEntry>();

        public List<ItemChanceEntry> CustomItemChances { get; set; } = new List<ItemChanceEntry>();

        [XmlArrayItem("ClothingSlot")]
        public List<ClothingSlotRule> ClothingRules { get; set; } = new List<ClothingSlotRule>();

        [XmlIgnore]
        private Dictionary<string, double> _regionChanceMap;

        [XmlIgnore]
        private Dictionary<ushort, double> _itemChanceMap;

        [XmlIgnore]
        private Dictionary<SlotType, ClothingSlotRule> _clothingRuleMap;

        public static DropRuleSet CreateDefault()
        {
            return new DropRuleSet
            {
                GlobalDefaultChance = 0.5d,
                RegionChances = new List<RegionChanceEntry>
                {
                    new RegionChanceEntry { Region = nameof(SlotType.PrimaryWeapon), Chance = 0.3d },
                    new RegionChanceEntry { Region = nameof(SlotType.SecondaryWeapon), Chance = 0.4d },
                    new RegionChanceEntry { Region = nameof(SlotType.Hands), Chance = 0.5d }
                },
                CustomItemChances = new List<ItemChanceEntry>(),
                ClothingRules = new List<ClothingSlotRule>
                {
                    new ClothingSlotRule { Slot = SlotType.Shirt, SlotDropChance = 0.0d, ContentsDropChance = 0.35d },
                    new ClothingSlotRule { Slot = SlotType.Pants, SlotDropChance = 0.0d, ContentsDropChance = 0.35d },
                    new ClothingSlotRule { Slot = SlotType.Backpack, SlotDropChance = 0.5d, ContentsDropChance = 0.5d },
                    new ClothingSlotRule { Slot = SlotType.Vest, SlotDropChance = 0.6d, ContentsDropChance = 0.45d },
                    new ClothingSlotRule { Slot = SlotType.Hat, SlotDropChance = 0.3d },
                    new ClothingSlotRule { Slot = SlotType.Mask, SlotDropChance = 0.3d },
                    new ClothingSlotRule { Slot = SlotType.Glasses, SlotDropChance = 0.3d }
                }
            };
        }

        public DropRuleSet NormalizedCopy()
        {
            var ruleSet = new DropRuleSet
            {
                GlobalDefaultChance = UtilityHelper.ClampChance(GlobalDefaultChance),
                RegionChances = new List<RegionChanceEntry>(),
                CustomItemChances = new List<ItemChanceEntry>(),
                ClothingRules = new List<ClothingSlotRule>()
            };

            if (RegionChances != null)
            {
                foreach (var entry in RegionChances)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Region))
                    {
                        continue;
                    }

                    ruleSet.RegionChances.Add(new RegionChanceEntry
                    {
                        Region = entry.Region.Trim(),
                        Chance = UtilityHelper.ClampChance(entry.Chance)
                    });
                }
            }

            if (CustomItemChances != null)
            {
                foreach (var entry in CustomItemChances)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    ruleSet.CustomItemChances.Add(new ItemChanceEntry
                    {
                        ItemID = entry.ItemID,
                        Chance = UtilityHelper.ClampChance(entry.Chance)
                    });
                }
            }

            if (ClothingRules != null)
            {
                foreach (var rule in ClothingRules)
                {
                    if (rule == null || rule.Slot == SlotType.Unknown)
                    {
                        continue;
                    }

                    // Hat, Mask, Glasses don't have storage, so ignore ContentsDropChance
                    var contentsChance = (rule.Slot == SlotType.Hat || rule.Slot == SlotType.Mask || rule.Slot == SlotType.Glasses)
                        ? 0.0d
                        : UtilityHelper.ClampChance(rule.ContentsDropChance);

                    ruleSet.ClothingRules.Add(new ClothingSlotRule
                    {
                        Slot = rule.Slot,
                        SlotDropChance = UtilityHelper.ClampChance(rule.SlotDropChance),
                        ContentsDropChance = contentsChance
                    });
                }
            }

            ruleSet.ResetCaches();
            return ruleSet;
        }

        public double ResolveChance(SlotType slotType, ushort itemId, out string source)
        {
            EnsureCaches();

            if (_itemChanceMap.TryGetValue(itemId, out var customChance))
            {
                source = $"Item:{itemId}";
                return customChance;
            }

            var slotKey = slotType.ToString();
            if (_regionChanceMap.TryGetValue(slotKey, out var regionChance))
            {
                source = $"Region:{slotKey}";
                return regionChance;
            }

            source = "Global";
            return UtilityHelper.ClampChance(GlobalDefaultChance);
        }

        public ClothingSlotRule ResolveClothingRule(SlotType slotType)
        {
            EnsureCaches();

            if (_clothingRuleMap != null && _clothingRuleMap.TryGetValue(slotType, out var rule))
            {
                return rule;
            }

            return new ClothingSlotRule
            {
                Slot = slotType,
                SlotDropChance = UtilityHelper.ClampChance(GlobalDefaultChance),
                ContentsDropChance = UtilityHelper.ClampChance(GlobalDefaultChance)
            };
        }

        private void EnsureCaches()
        {
            if (_regionChanceMap != null && _itemChanceMap != null)
            {
                return;
            }

            _regionChanceMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (RegionChances != null)
            {
                foreach (var entry in RegionChances)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Region))
                    {
                        continue;
                    }

                    _regionChanceMap[entry.Region.Trim()] = UtilityHelper.ClampChance(entry.Chance);
                }
            }

            _itemChanceMap = new Dictionary<ushort, double>();
            if (CustomItemChances != null)
            {
                foreach (var entry in CustomItemChances)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    _itemChanceMap[entry.ItemID] = UtilityHelper.ClampChance(entry.Chance);
                }
            }

            _clothingRuleMap = new Dictionary<SlotType, ClothingSlotRule>();
            if (ClothingRules != null)
            {
                foreach (var rule in ClothingRules)
                {
                    if (rule == null || rule.Slot == SlotType.Unknown)
                    {
                        continue;
                    }

                    // Hat, Mask, Glasses don't have storage, so ignore ContentsDropChance
                    var contentsChance = (rule.Slot == SlotType.Hat || rule.Slot == SlotType.Mask || rule.Slot == SlotType.Glasses)
                        ? 0.0d
                        : UtilityHelper.ClampChance(rule.ContentsDropChance);

                    var normalized = new ClothingSlotRule
                    {
                        Slot = rule.Slot,
                        SlotDropChance = UtilityHelper.ClampChance(rule.SlotDropChance),
                        ContentsDropChance = contentsChance
                    };
                    _clothingRuleMap[normalized.Slot] = normalized;
                }
            }
        }

        private void ResetCaches()
        {
            _regionChanceMap = null;
            _itemChanceMap = null;
            _clothingRuleMap = null;
        }

    }

    public class RegionChanceEntry
    {
        public string Region { get; set; }

        public double Chance { get; set; }
    }

    public class ItemChanceEntry
    {
        public ushort ItemID { get; set; }

        public double Chance { get; set; }
    }

    public class ClothingSlotRule
    {
        public SlotType Slot { get; set; }

        public double SlotDropChance { get; set; } = 0.5d;

        public double ContentsDropChance { get; set; } = 0.5d;
    }
}

