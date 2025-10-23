using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Services.Mod;
using System.Reflection;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Logger;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Utils.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Routers;
using System.IO;
using SPTarkov.Server.Core.Models.Spt.Templates;
namespace VulcanCore;
public class AssortUtils
{
    public static void InitAssortData(List<CustomAssortData> assortData, DatabaseService databaseService, ICloner cloner)
    {
        foreach (CustomAssortData assort in assortData)
        {
            switch (assort)
            {
                case CustomNormalAssortData customAssortData:
                    {
                        InitAssort(assort, databaseService, cloner);
                    }
                    break;
                case CustomLockedAssortData customLockedAssortData:
                    {
                        var assortUnlockRewardData = new CustomAssortUnlockRewardData
                        {
                            Id = (MongoId)VulcanUtil.ConvertHashID($"{customLockedAssortData.Id}_Locked"),
                            QuestId = (MongoId)VulcanUtil.ConvertHashID(customLockedAssortData.QuestId),
                            QuestStage = customLockedAssortData.QuestStage,
                            IsUnknownReward = customLockedAssortData.IsUnknownReward,
                            AssortData = customLockedAssortData,
                        };
                        QuestUtils.InitAssortUnlockRewards(assortUnlockRewardData, databaseService, cloner);
                    }
                    break;
            }
        }
    }

    public static void InitAssort(CustomAssortData assortData, DatabaseService databaseService, ICloner cloner)
    {
        var assort = assortData;
        var assortid = VulcanUtil.ConvertHashID(assort.Id);
        var traderassort = TraderUtils.GetTrader((string)assort.Trader, databaseService).Assort;
        var items = ItemUtils.ConvertItemListData(assort.Item, cloner);
        foreach (Item item in items)
        {
            traderassort.Items.Add(item);
        }
        var barterlist = new List<List<BarterScheme>> {
            new List<BarterScheme>()
        };
        foreach (var barter in assort.Barter)
        {
            barterlist[0].Add(
            new BarterScheme
            {
                Template = VulcanUtil.ConvertHashID(barter.Key),
                Count = (double)barter.Value
            });
        }
        foreach (var barter in assort.DogTag)
        {
            barterlist[0].Add(
            new BarterScheme
            {
                Template = VulcanUtil.ConvertHashID(barter.Key),
                Count = (double)barter.Value.Count,
                Level = barter.Value.Level,
                Side = (DogtagExchangeSide)barter.Value.Side
            });
        }
        traderassort.BarterScheme.Add((MongoId)assortid, barterlist);
        traderassort.LoyalLevelItems.Add((MongoId)assortid, assort.TrustLevel);
    }
}










