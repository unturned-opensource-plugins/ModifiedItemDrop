using System.Collections.Generic;
using System.Xml.Serialization;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class DeathSettings
    {
        [XmlArray("DeleteOnDeathItems")]
        [XmlArrayItem("ItemID")]
        public List<ushort> DeleteOnDeathItems { get; set; } = new List<ushort>();

        [XmlArray("RespawnItems")]
        [XmlArrayItem("Item")]
        public List<RespawnItem> RespawnItems { get; set; } = new List<RespawnItem>();

        public static DeathSettings CreateDefault()
        {
            return new DeathSettings();
        }
    }

    public class RespawnItem
    {
        [XmlAttribute("id")]
        public ushort ItemID { get; set; }

        [XmlAttribute("amount")]
        public byte Amount { get; set; } = 1;

        [XmlAttribute("quality")]
        public byte Quality { get; set; } = 100;
    }
}
