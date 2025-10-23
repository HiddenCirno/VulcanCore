using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Services.Mod;
using System.Reflection;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Logger;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Utils.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Common.Extensions;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SPTarkov.Server.Core.Models.Enums;

namespace VulcanCore;


public class ItemUtils
{
    public static TemplateItem GetItem(string itemid, DatabaseService databaseService)
    {
        return databaseService.GetItems().FirstOrDefault(x => x.Value.Id == (MongoId)itemid).Value;
    }
    public static MongoId? GetItemRagfairTag(string itemid, DatabaseService databaseService)
    {
        var handbook = databaseService.GetHandbook();
        var item = handbook.Items.FirstOrDefault(x => x.Id == (MongoId)itemid);
        if (item != null)
        {
            return item.ParentId;
        }
        return null;
    }
    public static int GetItemMinPrice(
        string itemid,
        DatabaseService databaseService,
        RagfairController ragfairController,
        RagfairOfferService ragfairOfferService,
        ItemHelper itemHelper,
        VulcanCore corePostDBLoad)
    {
        var item = GetItem(itemid, databaseService);
        var itemsid = (MongoId)itemid;
        var priceTable = databaseService.GetPrices();
        var handbook = databaseService.GetHandbook().Items;
        var offers = corePostDBLoad.GetItemMinAvgMaxFleaPriceValues(new GetMarketPriceRequestData { TemplateId = itemsid });
        //var ragfairPrice = offers.Min;
        var ragfairPrice = GetRagfairPrice(itemid, databaseService, ragfairController, ragfairOfferService, itemHelper);
        var tablePrice = (int)priceTable.FirstOrDefault(kv => kv.Key == itemsid).Value;
        if (ragfairPrice > 0)
        {
            return (int)ragfairPrice;
        }
        else if (tablePrice > 0)
        {
            return tablePrice;
        }
        else
        {
            var handbookdata = handbook.FirstOrDefault(i => i.Id == itemsid);
            if (handbookdata != null && handbookdata.Price > 0)
            {
                return (int)(handbookdata.Price * 0.6);
            }
            else return 1;
        }
    }
    public static int GetRagfairPrice(string itemid, DatabaseService databaseService, RagfairController ragfairController, RagfairOfferService ragfairOfferService, ItemHelper itemHelper)
    {
        var mongoid = (MongoId)itemid;
        IEnumerable<RagfairOffer> offersOfType = ragfairOfferService.GetOffersOfType(mongoid);
        if (offersOfType != null)
        {
            var offer = offersOfType
                .Where(x => x.User.MemberType != MemberCategory.Trader
                && x.Requirements.ToList()[0].TemplateId == "5449016a4bdc2d6f028b456f"
                && itemHelper.GetItemQualityModifier(x.Items[0]) == 1
                && (x.Items.Count > 1 || !(bool)x.SellInOnePiece))
            .ToList();
            if (offer.Count > 0)
            {
                var totalCost = offer.Sum(x => x.SummaryCost);
                var averageCost = offer.Count > 0 ? totalCost / offer.Count : 0;
                return (int)Math.Floor((double)averageCost);
            }
        }
        return 0;
    }
    public static void InitItem(Dictionary<string, CustomItemTemplate> items, string creator, string modname, ISptLogger<VulcanCore> logger, DatabaseService databaseService, ICloner cloner, ConfigServer configServer)
    {
        foreach (var item in items)
        {
            CreateAndAddItem(item.Value, item.Value.TargetId, creator, modname, logger, databaseService, cloner, configServer);
        }
    }
    public static void CreateAndAddItem(CustomItemTemplate template, string targetid, string creator, string modname, ISptLogger<VulcanCore> logger, DatabaseService databaseService, ICloner cloner, ConfigServer configServer)
    {
        //TemplateItem itemClone = VulcanUtil.DeepCopyJson(GetItem(targetid, databaseService));
        //物品模板复制
        TemplateItem itemClone = cloner.Clone(GetItem(targetid, databaseService));
        //原版属性覆盖
        VulcanUtil.CopyNonNullProperties(template.Props, itemClone.Properties);
        var itemid = VulcanUtil.ConvertHashID(template.Id);
        template.Id = itemid;
        //参数覆盖
        SetItemBaseData(template, itemClone);
        //CustomItemService.CreateItemFromClone();
        var _inventoryConfig = configServer.GetConfig<InventoryConfig>();
        //自定义货币处理
        if (template.CustomProps.IsMoney)
        {
            _inventoryConfig.CustomMoneyTpls.Add((MongoId)itemid);
        }
        //跳蚤市场价格处理
        if (template.CustomProps?.RagfairPrice != null)
        {
            databaseService.GetPrices()[itemid] = (double)template.CustomProps.RagfairPrice;
        }
        //Buff物品处理
        AddBuffItemData(template, configServer, databaseService);
        //黑名单处理
        if (template.CustomProps?.BlackListType != null)
        {
            AddBlackList(template, configServer);
        }
        //手册数据
        AddHandbookData(template, databaseService);
        //武器相关
        AddWeaponItemData(template, databaseService);
        //任务物品
        AddQuestItemGeneaate(template, databaseService, logger, cloner);
        //test
        LootUtils.AddStaticLoot(template, databaseService, logger);
        LootUtils.AddLooseLoot(template, databaseService, logger);
        //本地化数据
        var Locales = BuildItemLocales(template.CustomProps, creator, modname);
        LocaleUtils.AddItemToLocales(Locales, itemid, databaseService);
        //尝试添加物品
        databaseService.GetItems().TryAdd(itemid, itemClone);
        VulcanLog.Debug($"物品添加成功: {template.CustomProps.Name}", logger);
    }
    public static Dictionary<string, LocaleDetails> BuildItemLocales(CustomProps props, string creator, string modname)
    {
        var locales = new Dictionary<string, LocaleDetails>();
        var modstring = $"<color=#FFFFFF><b>\n由{creator}创建\n添加者: {modname}\n物品API：火神之心\n物品ID：#ItemId</b></color>";
        var chdescription = $"{props.Description}{modstring}";
        // 默认中文
        locales["ch"] = new LocaleDetails
        {
            Name = props.Name,
            ShortName = props.ShortName,
            Description = chdescription
        };

        // 英文（优先英文字段）
        locales["en"] = new LocaleDetails
        {
            Name = string.IsNullOrEmpty(props.EName) ? props.Name : props.EName,
            ShortName = string.IsNullOrEmpty(props.EShortName) ? props.ShortName : props.EShortName,
            Description = string.IsNullOrEmpty(props.EDescription) ? chdescription : props.EDescription
        };

        // 日语（默认中文）
        locales["jp"] = new LocaleDetails
        {
            Name = string.IsNullOrEmpty(props.JName) ? props.Name : props.JName,
            ShortName = string.IsNullOrEmpty(props.JShortName) ? props.ShortName : props.JShortName,
            Description = string.IsNullOrEmpty(props.JDescription) ? chdescription : props.JDescription
        };

        return locales;
    }
    public static void AddBlackList(CustomItemTemplate template, ConfigServer configServer)
    {
        List<string> blacklist = BitMapUtils.GetBlackListCode(template.CustomProps.BlackListType);
        foreach (string black in blacklist)
        {
            string itemid = template.Id;
            switch (black)
            {
                case "AirDrop":
                    {
                        AddAirDropBlackList(itemid, configServer);
                    }
                    break;
                case "PMCLoot":
                    {
                        AddPMCLootBlackList(itemid, configServer);
                    }
                    break;
                case "ScavCaseLoot":
                    {
                        AddScavCaseLootBlackList(itemid, configServer);
                    }
                    break;
                case "Fence":
                    {
                        AddFenceBlackList(itemid, configServer);
                    }
                    break;
                case "Circle":
                    {
                        AddCircleBlackList(itemid, configServer);
                    }
                    break;
            }
        }
    }
    public static void AddAirDropBlackList(string itemid, ConfigServer configserver)
    {
        AirdropConfig lootConfig = configserver.GetConfig<AirdropConfig>();
        foreach (AirdropLoot loot in lootConfig.Loot.Values)
        {
            loot.ItemBlacklist.Add(itemid);
        }
    }
    public static void AddPMCLootBlackList(string itemid, ConfigServer configserver)
    {
        PmcConfig lootConfig = configserver.GetConfig<PmcConfig>();
        lootConfig.VestLoot.Blacklist.Add(itemid);
        lootConfig.PocketLoot.Blacklist.Add(itemid);
        lootConfig.BackpackLoot.Blacklist.Add(itemid);
    }
    public static void AddScavCaseLootBlackList(string itemid, ConfigServer configserver)
    {
        ScavCaseConfig lootConfig = configserver.GetConfig<ScavCaseConfig>();
        lootConfig.RewardItemBlacklist.Add(itemid);
    }
    public static void AddFenceBlackList(string itemid, ConfigServer configserver)
    {
        TraderConfig lootConfig = configserver.GetConfig<TraderConfig>();
        lootConfig.Fence.Blacklist.Add(itemid);
    }
    public static void AddCircleBlackList(string itemid, ConfigServer configserver)
    {
        HideoutConfig lootConfig = configserver.GetConfig<HideoutConfig>();
        lootConfig.CultistCircle.RewardItemBlacklist.Add(itemid);
    }
    public static void AddBuffItemData(CustomItemTemplate template, ConfigServer configserver, DatabaseService databaseService)
    {
        Globals globals = databaseService.GetGlobals();
        if (template.CustomProps is BuffItemProps itemProps)
        {
            globals.Configuration.Health.Effects.Stimulator.Buffs[template.Props.StimulatorBuffs] = itemProps.BuffValue;
        }
    }
    public static void AddHandbookData(CustomItemTemplate template, DatabaseService databaseService)
    {
        string itemid = template.Id;
        databaseService.GetTemplates().Handbook.Items.Add(new HandbookItem
        {
            Id = itemid,
            ParentId = VulcanUtil.ConvertHashID(template.CustomProps.RagfairType),
            Price = (double)template.CustomProps.DefaultPrice
        });
    }
    public static void SetItemBaseData(CustomItemTemplate template, TemplateItem item)
    {
        item.Id = template.Id;
        item.Parent = template.ParentId != null ? template.ParentId : item.Parent;
        if (item.Prototype != null)
        {
            item.Prototype = template.Prototype != null ? template.Prototype : item.Prototype;
        }
        item.Type = template.Type != null ? template.Type : item.Type;
    }
    public static void AddWeaponItemData(CustomItemTemplate template, DatabaseService databaseService)
    {
        if (template.CustomProps is WeaponItemProps itemProps)
        {
            if (itemProps?.FixMastering == true)
            {
                FixWeaponMastering(template, databaseService);
            }
            if (itemProps?.AddMastering == true)
            {
                AddWeaponMastering(template, databaseService);
            }
        }
    }
    public static void FixWeaponMastering(CustomItemTemplate template, DatabaseService databaseService)
    {
        Globals globals = databaseService.GetGlobals();
        foreach (Mastering mastering in globals.Configuration.Mastering)
        {
            WeaponItemProps itemProps = (WeaponItemProps)template.CustomProps;
            if (itemProps?.CustomMasteringTarget != null)
            {
                if (mastering.Templates.Contains(itemProps.CustomMasteringTarget))
                {
                    List<MongoId> list = mastering.Templates?.ToList() ?? new List<MongoId>();
                    list.Add((MongoId)template.Id); // 添加新元素
                    mastering.Templates = list;
                }
            }
            else
            {
                if (mastering.Templates.Contains(template.TargetId))
                {
                    List<MongoId> list = mastering.Templates?.ToList() ?? new List<MongoId>();
                    list.Add((MongoId)template.Id); // 添加新元素
                    mastering.Templates = list;
                }
            }

        }
    }
    public static void AddWeaponMastering(CustomItemTemplate template, DatabaseService databaseService)
    {
        Globals globals = databaseService.GetGlobals();
        WeaponItemProps itemProps = (WeaponItemProps)template.CustomProps;
        globals.Configuration.Mastering = VulcanUtil.AddToArray<Mastering>(globals.Configuration.Mastering, itemProps.Mastering);
    }
    public static void AddQuestItemGeneaate(CustomItemTemplate template, DatabaseService databaseService, ISptLogger<VulcanCore> logger, ICloner cloner)
    {
        if (template.CustomProps is QuestItemProps questItemProps)
        {
            //VulcanLog.Debug("111", logger);
            var spawnpoint = questItemProps.SpawnPointData;
            var looseloot = databaseService.GetLocation(spawnpoint.Location)?.LooseLoot;
            if (looseloot != null)
            {
                looseloot.AddTransformer(delegate (LooseLoot loostLoot)
                {
                    VulcanLog.Debug(loostLoot.SpawnpointsForced.Count().ToString(), logger);
                    spawnpoint.Template.Root = VulcanUtil.ConvertHashID(spawnpoint.Template.Root);
                    var list = loostLoot.SpawnpointsForced.ToList();
                    var newspawnpoint = new Spawnpoint
                    {
                        LocationId = spawnpoint.LocationId,
                        Probability = spawnpoint.Probability,
                        Template = new SpawnpointTemplate
                        {
                            Id = spawnpoint.Template.Id,
                            IsAlwaysSpawn = spawnpoint.Template.IsAlwaysSpawn,
                            IsGroupPosition = spawnpoint.Template.IsGroupPosition,
                            GroupPositions = spawnpoint.Template.GroupPositions,
                            Position = spawnpoint.Template.Position,
                            Rotation = spawnpoint.Template.Rotation,
                            Root = spawnpoint.Template.Root,
                            Items = new List<SptLootItem>()
                        }
                    };
                    var spawnpointitemlist = newspawnpoint.Template.Items.ToList();
                    foreach (var item in spawnpoint.Template.Items)
                    {
                        spawnpointitemlist.Add(new SptLootItem
                        {
                            Id = item.Id,
                            Template = item.Template
                        });
                        VulcanLog.Debug(spawnpoint.Template.Root, logger);
                        VulcanLog.Debug(item.Id, logger);
                    }
                    newspawnpoint.Template.Items = spawnpointitemlist;
                    list.Add(newspawnpoint);
                    loostLoot.SpawnpointsForced = list;
                    VulcanLog.Debug(loostLoot.SpawnpointsForced.Count().ToString(), logger);
                    return loostLoot;
                });
            }
        }
    }
    public static List<Item> ConvertItemListData(List<CustomItem> itemlist, ICloner cloner)
    {
        var list = new List<Item>();
        foreach (CustomItem item in itemlist)
        {
            var copyitem = cloner.Clone(item);
            if (copyitem.ParentId != null && copyitem.ParentId != "hideout")
            {
                copyitem.ParentId = VulcanUtil.ConvertHashID(copyitem.ParentId);
            }
            list.Add((Item)copyitem);
        }
        return list;
    }
    public static List<Item> RegenerateItemListData(List<Item> itemlist, string addinfo, ICloner cloner)
    {
        var list = new List<Item>();
        foreach (CustomItem item in itemlist)
        {
            var copyitem = cloner.Clone(item);
            copyitem.Id = VulcanUtil.ConvertHashID($"{copyitem.Id}_{addinfo}");
            if (copyitem.ParentId != null && copyitem.ParentId != "hideout")
            {
                copyitem.ParentId = VulcanUtil.ConvertHashID($"{copyitem.ParentId}_{addinfo}");
            }
            list.Add(copyitem);
        }
        return list;
    }
}










