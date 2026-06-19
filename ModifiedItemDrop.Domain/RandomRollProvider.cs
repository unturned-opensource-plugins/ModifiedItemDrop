using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    internal sealed class RandomRollProvider : IRollProvider
    {
        private readonly Random _random = new Random();

        public double NextRoll()
        {
            return _random.NextDouble();
        }
    }
}
