using System.Collections.Generic;
using System.Xml.Serialization;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    /// <summary>
    /// Configuration for hands slot size based on player permissions.
    /// Permission format: ModifiedItemDrop.Hands.{PermissionName}
    /// </summary>
    public class HandsSlotSettings
    {
        [XmlArrayItem("HandsConfig")]
        public List<HandsConfig> Configurations { get; set; } = new List<HandsConfig>();

        public static HandsSlotSettings CreateDefault()
        {
            return new HandsSlotSettings
            {
                Configurations = new List<HandsConfig>
                {
                    new HandsConfig
                    {
                        PermissionName = "default",
                        Width = 4,
                        Height = 4
                    }
                }
            };
        }
    }

    public class HandsConfig
    {
        /// <summary>
        /// Permission name (without "ModifiedItemDrop.Hands." prefix)
        /// </summary>
        [XmlAttribute("permission")]
        public string PermissionName { get; set; }

        /// <summary>
        /// Width of hands slot
        /// </summary>
        [XmlAttribute("width")]
        public byte Width { get; set; }

        /// <summary>
        /// Height of hands slot
        /// </summary>
        [XmlAttribute("height")]
        public byte Height { get; set; }
    }
}
