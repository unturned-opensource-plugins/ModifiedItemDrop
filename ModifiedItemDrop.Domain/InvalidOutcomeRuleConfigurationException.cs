using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class InvalidOutcomeRuleConfigurationException : Exception
    {
        public InvalidOutcomeRuleConfigurationException(string message)
            : base(message)
        {
        }
    }
}
