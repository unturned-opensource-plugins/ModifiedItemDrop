using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class HandsSlotCapabilityRule
    {
        public HandsSlotCapabilityRule(string permissionName, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
            {
                throw new ArgumentException("Hands slot capability permission name must be provided.", nameof(permissionName));
            }

            PermissionName = permissionName;
            Width = width;
            Height = height;
        }

        public string PermissionName { get; }

        public int Width { get; }

        public int Height { get; }

        public string Permission => "ModifiedItemDrop.Hands." + PermissionName;
    }
}
