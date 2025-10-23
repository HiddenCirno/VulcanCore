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

namespace VulcanCore;


public class TraderUtils
{
    public static Trader GetTrader(string traderid, DatabaseService databaseService)
    {
        return databaseService.GetTraders().FirstOrDefault(x => x.Value.Base.Id == (MongoId)traderid).Value;
    }
    public static void InitTrader(TraderBaseWithDesc traderBase, string imagePath, string creator, string modname, ConfigServer configServer, DatabaseService databaseService, ICloner cloner, ImageRouter imageRouter)
    {
        InsuranceConfig insuranceConfig = configServer.GetConfig<InsuranceConfig>();

        //TraderBase traderBasePattern = cloner.Clone(GetTrader(TraderType.Prapor, databaseService).Base);
        Trader TraderPattern = cloner.Clone(GetTrader((string)Traders.PRAPOR, databaseService));
        //imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), Path.Combine(imagePath, Path.GetFileName(traderBase.Avatar)));
        ImageUtils.RegisterImageRoute(traderBase.Avatar.Replace(".jpg", ""), Path.Combine(imagePath, Path.GetFileName(traderBase.Avatar)), imageRouter);
        traderBase.Id = (MongoId)traderBase.Id;
        TraderPattern.Assort.Items?.Clear();
        TraderPattern.Assort.BarterScheme?.Clear();
        TraderPattern.Assort.LoyalLevelItems?.Clear();
        //TraderPattern.QuestAssort.Clear();
        TraderPattern.QuestAssort["started"].Clear();
        TraderPattern.QuestAssort["success"].Clear();
        TraderPattern.QuestAssort["fail"].Clear();
        TraderPattern.Dialogue.Clear();
        TraderPattern?.Suits?.Clear();
        TraderPattern?.Services?.Clear();
        VulcanUtil.CopyNonNullProperties(traderBase, TraderPattern.Base);
        LocaleUtils.AddTraderToLocales(traderBase, databaseService, creator, modname);
        databaseService.GetTables().Traders[traderBase.Id] = TraderPattern;
        //Traders
    }
    
}










