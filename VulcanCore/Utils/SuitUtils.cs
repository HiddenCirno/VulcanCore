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
public class SuitUtils
{
    
    public static void InitCustomSuitData(List<CustomSuit> customSuits, DatabaseService databaseService, ICloner cloner)
    {
        foreach (var suit in customSuits)
        {
            InitCustomSuit(suit, databaseService, cloner);
        }
    }
    public static void InitCustomSuitData(List<CustomSuit> customSuits, MongoId traderId, DatabaseService databaseService, ICloner cloner)
    {
        foreach (var suit in customSuits)
        {
            InitCustomSuit(suit, traderId, databaseService, cloner);
        }
    }
    public static void InitCustomSuit(CustomSuit customSuit, DatabaseService databaseService, ICloner cloner)
    {
        var suits = databaseService.GetTrader(Traders.RAGMAN).Suits;
        var suit = new Suit
        {
            Id = customSuit.Id,
            SuiteId = customSuit.SuiteId,
            Tid = customSuit.Tid,
            IsActive = customSuit.IsActive,
            InternalObtain = customSuit.InternalObtain,
            IsHiddenInPVE = customSuit.IsHiddenInPVE,
            ExternalObtain = customSuit.ExternalObtain,
            RelatedBattlePassSeason = customSuit.RelatedBattlePassSeason,
            Requirements = new SuitRequirements
            {
                LoyaltyLevel = customSuit.Requirements.LoyaltyLevel,
                PrestigeLevel = customSuit.Requirements.PrestigeLevel,
                ProfileLevel = customSuit.Requirements.ProfileLevel,
                Standing = customSuit.Requirements.Standing,
                RequiredTid = customSuit.Requirements.RequiredTid,
                SkillRequirements = new List<string>(),
                AchievementRequirements = new List<string>(),
                ItemRequirements = customSuit.Requirements.ItemRequirements,
                QuestRequirements = new List<string>()
            }
        };
        foreach (var key in customSuit.Requirements.QuestRequirements)
        {
            suit.Requirements.QuestRequirements.Add(VulcanUtil.ConvertHashID(key));
        }
        suits.Add(suit);
    }
    public static void InitCustomSuit(CustomSuit customSuit, MongoId traderId, DatabaseService databaseService, ICloner cloner)
    {
        var suits = databaseService.GetTrader(traderId).Suits;
        var suit = new Suit
        {
            Id = customSuit.Id,
            SuiteId = customSuit.SuiteId,
            Tid = customSuit.Tid,
            IsActive = customSuit.IsActive,
            InternalObtain = customSuit.InternalObtain,
            IsHiddenInPVE = customSuit.IsHiddenInPVE,
            ExternalObtain = customSuit.ExternalObtain,
            RelatedBattlePassSeason = customSuit.RelatedBattlePassSeason,
            Requirements = new SuitRequirements
            {
                LoyaltyLevel = customSuit.Requirements.LoyaltyLevel,
                PrestigeLevel = customSuit.Requirements.PrestigeLevel,
                ProfileLevel = customSuit.Requirements.ProfileLevel,
                Standing = customSuit.Requirements.Standing,
                RequiredTid = customSuit.Requirements.RequiredTid,
                SkillRequirements = new List<string>(),
                AchievementRequirements = new List<string>(),
                ItemRequirements = customSuit.Requirements.ItemRequirements,
                QuestRequirements = new List<string>()
            }
        };
        foreach (var key in customSuit.Requirements.QuestRequirements)
        {
            suit.Requirements.QuestRequirements.Add(VulcanUtil.ConvertHashID(key));
        }
        suits.Add(suit);
    }
}










