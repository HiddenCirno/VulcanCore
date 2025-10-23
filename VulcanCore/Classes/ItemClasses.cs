using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using System.Text.Json.Serialization;
using static VulcanCore.VulcanUtil;

namespace VulcanCore;

public class CustomItemTemplate
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    [JsonPropertyName("_targetid")]
    public string TargetId { get; set; }
    [JsonPropertyName("_parent")]
    public string ParentId { get; set; }
    [JsonPropertyName("_name")]
    public string Name { get; set; }

    // 自定义参数
    [JsonPropertyName("_customprops")]
    public CustomProps CustomProps { get; set; }

    // 原版 Props
    [JsonPropertyName("_props")]
    public TemplateItemProperties Props { get; set; }
    [JsonPropertyName("_proto")]
    public string Prototype { get; set; }
    [JsonPropertyName("_type")]
    public string Type { get; set; }
}

// 自定义参数类
[JsonDerivedType(typeof(CustomProps), "base")]
[JsonDerivedType(typeof(CustomFixedItemProps), "fixed")]
[JsonDerivedType(typeof(WeaponItemProps), "weapon")]
[JsonDerivedType(typeof(LootableItemProps), "lootable")]
[JsonDerivedType(typeof(GiftBoxProps), "giftbox")]
[JsonDerivedType(typeof(BuffItemProps), "buff")]
[JsonDerivedType(typeof(QuestItemProps), "quest")]
public class CustomProps
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("ShortName")]
    public string ShortName { get; set; }

    [JsonPropertyName("Description")]
    public string Description { get; set; }

    [JsonPropertyName("EName")]
    public string EName { get; set; }

    [JsonPropertyName("EShortName")]
    public string EShortName { get; set; }

    [JsonPropertyName("EDescription")]
    public string EDescription { get; set; }

    [JsonPropertyName("JName")]
    public string JName { get; set; }

    [JsonPropertyName("JShortName")]
    public string JShortName { get; set; }

    [JsonPropertyName("JDescription")]
    public string JDescription { get; set; }

    [JsonPropertyName("DefaultPrice")]
    public int DefaultPrice { get; set; }

    [JsonPropertyName("RagfairPrice")]
    public int RagfairPrice { get; set; }

    [JsonPropertyName("RagfairType")]
    public string RagfairType { get; set; }

    [JsonPropertyName("isMoney")]
    public bool IsMoney { get; set; }

    [JsonPropertyName("addToKappa")]
    public bool AddToKappa { get; set; }

    [JsonPropertyName("BlackListType")]
    public int BlackListType { get; set; }
    [JsonPropertyName("SafeMode")]
    public bool SafeMode { get; set; }
    [JsonExtensionData]
    public Dictionary<string?, object?>? ExtensionData => _extensionData;

    [JsonIgnore]
    private readonly Dictionary<string?, object?>? _extensionData = new Dictionary<string, object>();
}
public class CustomFixedItemProps : CustomProps
{
    public string CustomFixID { get; set; }
    public string[] FixType { get; set; }

}

public class WeaponItemProps : CustomFixedItemProps
{
    [JsonPropertyName("FixMastering")]
    public bool FixMastering { get; set; }
    [JsonPropertyName("AddMastering")]
    public bool AddMastering { get; set; }
    [JsonPropertyName("Mastering")]
    public Mastering Mastering { get; set; }

    [JsonPropertyName("CustomMasteringTarget")]
    public string CustomMasteringTarget { get; set; }

}
public class LootableItemProps : CustomFixedItemProps
{
    [JsonPropertyName("CanFindInRaid")]
    public bool CanFindInRaid { get; set; }
    [JsonPropertyName("CustomLoot")]
    public bool UseCustomData { get; set; }
    [JsonPropertyName("MapLoot")]
    public bool MapLoot { get; set; }
    [JsonPropertyName("CustomMapLootTarget")]
    public string CustomMapLootTarget { get; set; }
    [JsonPropertyName("MapLootDivisor")]
    public int MapLootDivisor { get; set; }
    [JsonPropertyName("StaticLoot")]
    public bool StaticLoot { get; set; }
    [JsonPropertyName("CustomStaticLootTarget")]
    public string CustomStaticLootTarget { get; set; }
    [JsonPropertyName("StaticLootDivisor")]
    public int StaticLootDivisor { get; set; }
}
public class GiftBoxProps : CustomFixedItemProps
{

}
public class BuffItemProps : LootableItemProps
{
    [JsonPropertyName("BuffValue")]
    public List<Buff> BuffValue { get; set; }
}
public class QuestItemProps : CustomProps
{
    [JsonPropertyName("QuestItemData")]
    public CustomSpawnPointData SpawnPointData { get; set; }
}

public record CustomSpawnPointData
{
    [JsonPropertyName("locationId")]
    public virtual string? LocationId { get; set; }

    [JsonPropertyName("probability")]
    public virtual double? Probability { get; set; }

    [JsonPropertyName("template")]
    public virtual CustomSpawnpointTemplate? Template { get; set; }
    [JsonPropertyName("location")]
    public string Location { get; set; }
}
public record CustomSpawnpointTemplate
{

    [JsonPropertyName("Id")]
    public virtual string? Id { get; set; }

    [JsonPropertyName("IsContainer")]
    public virtual bool? IsContainer { get; set; }

    [JsonPropertyName("useGravity")]
    public virtual bool? UseGravity { get; set; }

    [JsonPropertyName("randomRotation")]
    public virtual bool? RandomRotation { get; set; }

    [JsonPropertyName("Position")]
    public virtual XYZ? Position { get; set; }

    [JsonPropertyName("Rotation")]
    public virtual XYZ? Rotation { get; set; }

    [JsonPropertyName("IsAlwaysSpawn")]
    public virtual bool? IsAlwaysSpawn { get; set; }

    [JsonPropertyName("IsGroupPosition")]
    public virtual bool? IsGroupPosition { get; set; }

    [JsonPropertyName("GroupPositions")]
    public virtual IEnumerable<GroupPosition>? GroupPositions { get; set; }

    [JsonPropertyName("Root")]
    public virtual string? Root
    {
        get
        {
            return _root;
        }
        set
        {
            _root = ((value == null) ? null : string.Intern(value));
        }
    }

    [JsonPropertyName("Items")]
    public virtual List<CustomItem> Items { get; set; }

    [JsonExtensionData]
    public Dictionary<string?, object?>? ExtensionData => _extensionData;

    private string? _root;

    [JsonIgnore]
    private readonly Dictionary<string?, object?>? _extensionData = new Dictionary<string, object>();
}
public record CustomItem : Item
{
    [JsonPropertyName("_id")]
    [JsonConverter(typeof(MongoIdConverter))]
    public required override MongoId Id { get; set; }

    [JsonPropertyName("_tpl")]
    [JsonConverter(typeof(MongoIdConverter))]
    public required override MongoId Template { get; set; }
}

public class CustomDogTag
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    [JsonPropertyName("level")]
    public int Level { get; set; }
    [JsonPropertyName("side")]
    public int Side { get; set; }
}