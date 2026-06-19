using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class OutcomeRuleXmlParser
    {
        public static IReadOnlyList<OutcomeRule> Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new InvalidOutcomeRuleConfigurationException("Outcome Rules XML must be provided.");
            }

            var document = XDocument.Parse(xml);
            var root = document.Root;
            if (root == null)
            {
                throw new InvalidOutcomeRuleConfigurationException("Expected root element OutcomeRules.");
            }

            if (root.Name.LocalName != "OutcomeRules")
            {
                if (LooksLikeV1Configuration(root))
                {
                    throw new InvalidOutcomeRuleConfigurationException(
                        "Detected v1 ModifiedItemDrop configuration. v2 requires nested OutcomeRules XML; use the v1-to-v2 migration guide before enabling death processing.");
                }

                throw new InvalidOutcomeRuleConfigurationException("Expected root element OutcomeRules.");
            }

            return root.Elements("Rule").Select(ParseRule).ToList().AsReadOnly();
        }

        private static bool LooksLikeV1Configuration(XElement root)
        {
            if (root.Name.LocalName == "ModifiedItemDropConfiguration")
            {
                return true;
            }

            return root.Descendants("DeleteOnDeathItems").Any()
                || root.Descendants("DropChance").Any()
                || root.Descendants("RuleSet").Any();
        }

        private static OutcomeRule ParseRule(XElement ruleElement)
        {
            var name = RequiredAttribute(ruleElement, "name");
            var priority = int.Parse(RequiredAttribute(ruleElement, "priority"), CultureInfo.InvariantCulture);
            var outcome = RequiredElement(ruleElement, "Outcome");
            var outcomeKind = RequiredAttribute(outcome, "kind");

            if (outcomeKind == "Grant")
            {
                var trigger = ParseTrigger(RequiredElement(ruleElement, "Trigger"));
                return OutcomeRule.Grant(
                    name,
                    priority,
                    trigger,
                    ParseUShort(RequiredAttribute(outcome, "itemId"), "itemId"),
                    ParseByte(RequiredAttribute(outcome, "amount"), "amount"),
                    ParseByte(RequiredAttribute(outcome, "quality"), "quality"));
            }

            var target = ParseTarget(RequiredElement(ruleElement, "Target"));
            var chance = OptionalDoubleAttribute(outcome, "chance", 1.0);

            switch (outcomeKind)
            {
                case "Drop":
                    return OutcomeRule.Drop(name, priority, target, chance);
                case "Keep":
                    return OutcomeRule.Keep(name, priority, target, chance);
                case "Delete":
                    return OutcomeRule.Delete(name, priority, target);
                default:
                    throw new InvalidOutcomeRuleConfigurationException("Unsupported Outcome kind '" + outcomeKind + "'.");
            }
        }

        private static OutcomeRuleTriggerKind ParseTrigger(XElement triggerElement)
        {
            var kind = RequiredAttribute(triggerElement, "kind");
            switch (kind)
            {
                case "AfterDeathRespawn":
                    return OutcomeRuleTriggerKind.AfterDeathRespawn;
                default:
                    throw new InvalidOutcomeRuleConfigurationException("Unsupported Trigger kind '" + kind + "'.");
            }
        }

        private static OutcomeTarget ParseTarget(XElement targetElement)
        {
            var kind = RequiredAttribute(targetElement, "kind");
            switch (kind)
            {
                case "Any":
                    return OutcomeTarget.Any();
                case "Slot":
                    return OutcomeTarget.ForSlot(ParseSlot(RequiredAttribute(targetElement, "slot")));
                case "Item":
                    return OutcomeTarget.ForItem(ParseUShort(RequiredAttribute(targetElement, "itemId"), "itemId"));
                case "ClothingContent":
                    return OutcomeTarget.ForClothingContent(ParseSlot(RequiredAttribute(targetElement, "slot")));
                default:
                    throw new InvalidOutcomeRuleConfigurationException("Unsupported Target kind '" + kind + "'.");
            }
        }

        private static byte ParseByte(string value, string attributeName)
        {
            if (byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOutcomeRuleConfigurationException("Attribute " + attributeName + " must be an unsigned 8-bit integer.");
        }

        private static ushort ParseUShort(string value, string attributeName)
        {
            if (ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOutcomeRuleConfigurationException("Attribute " + attributeName + " must be an unsigned 16-bit integer.");
        }

        private static PlayerAssetSlot ParseSlot(string value)
        {
            if (Enum.TryParse(value, ignoreCase: false, result: out PlayerAssetSlot slot))
            {
                return slot;
            }

            throw new InvalidOutcomeRuleConfigurationException("Unsupported Player Asset slot '" + value + "'.");
        }

        private static XElement RequiredElement(XElement parent, string name)
        {
            var element = parent.Element(name);
            if (element == null)
            {
                throw new InvalidOutcomeRuleConfigurationException("Rule '" + RequiredAttribute(parent, "name") + "' must include " + name + ".");
            }

            return element;
        }

        private static string RequiredAttribute(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
            {
                throw new InvalidOutcomeRuleConfigurationException("Element " + element.Name.LocalName + " must include attribute " + name + ".");
            }

            return attribute.Value;
        }

        private static double OptionalDoubleAttribute(XElement element, string name, double fallback)
        {
            var attribute = element.Attribute(name);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
            {
                return fallback;
            }

            return double.Parse(attribute.Value, CultureInfo.InvariantCulture);
        }
    }
}
