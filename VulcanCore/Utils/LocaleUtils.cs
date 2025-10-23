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


public class LocaleUtils
{
    public static void InitQuestLocale(Dictionary<string, Dictionary<string, CustomQuestLocaleData>> customLocaleData, string creator, string modname, DatabaseService databaseService)
    {
        // 遍历语言，例如 ch / en / ru ...
        foreach (var languageEntry in customLocaleData)
        {
            string langKey = languageEntry.Key; // "ch"
            var quests = languageEntry.Value;   // Dictionary<string, CustomQuestLocaleData>

            // 获取目标语言对应的全局本地化 LazyLoad
            if (!databaseService.GetLocales().Global.TryGetValue(langKey, out LazyLoad<Dictionary<string, string>> lazyLocale))
                continue;

            // 为该语言添加 transformer（延迟加载时注入翻译数据）
            lazyLocale.AddTransformer(localeData =>
            {
                foreach (var questEntry in quests)
                {
                    string questId = VulcanUtil.ConvertHashID(questEntry.Key);
                    var modstring = $"<color=#FFFFFF><b>\n由{creator}创建\n添加者: {modname}\n商人API：火神之心\n任务ID：{questId}</b></color>";         // 例如 "PersicariaTask1"
                    var locale = questEntry.Value;                 // CustomQuestLocaleData 对象

                    // 写入任务主要字段
                    localeData[$"{questId} name"] = locale.QuestName;
                    localeData[$"{questId} description"] = $"{locale.QuestDescription}{modstring}";
                    localeData[$"{questId} note"] = locale.QuestNote ?? "";
                    localeData[$"{questId} failMessageText"] = locale.QuestFailMessage ?? "";
                    localeData[$"{questId} startedMessageText"] = locale.QuestStartMessaage ?? "";
                    localeData[$"{questId} successMessageText"] = locale.QuestSuccessMessage ?? "";
                    localeData[$"{questId} location"] = locale.QuestLocation ?? "";

                    // 写入每个条件文本（如 Hand/Find 条件）
                    if (locale.QuestConditions != null)
                    {
                        foreach (var cond in locale.QuestConditions)
                        {
                            // cond.Key = "PersicariaTask1Find1"
                            // cond.Value = "在战局中找到电线"
                            localeData[VulcanUtil.ConvertHashID(cond.Key)] = cond.Value;
                        }
                    }
                }

                return localeData;
            });
        }
    }
    public static void AddItemToLocales(Dictionary<string, LocaleDetails> localeDetails, string newItemId, DatabaseService databaseService)
    {
        string newItemId2 = newItemId;
        foreach (KeyValuePair<string, string> language in databaseService.GetLocales().Languages)
        {
            localeDetails.TryGetValue(language.Key, out LocaleDetails newLocaleDetails);
            if ((object)newLocaleDetails == null)
            {
                newLocaleDetails = localeDetails[localeDetails.Keys.FirstOrDefault()];
            }

            if (databaseService.GetLocales().Global.TryGetValue(language.Key, out LazyLoad<Dictionary<string, string>> value))
            {
                value.AddTransformer(delegate (Dictionary<string, string>? localeData)
                {
                    localeData.Add(newItemId2 + " Name", newLocaleDetails.Name);
                    localeData.Add(newItemId2 + " ShortName", newLocaleDetails.ShortName);
                    localeData.Add(newItemId2 + " Description", newLocaleDetails.Description.Replace("#ItemId", newItemId2));
                    return localeData;
                });
            }
        }
    }
    public static void AddTraderToLocales(TraderBaseWithDesc baseJson, DatabaseService databaseService, string creator, string modname)
    {
        var locales = databaseService.GetTables().Locales.Global;
        var newTraderId = baseJson.Id;
        var modstring = $"<color=#FFFFFF><b>\n由{creator}创建\n添加者: {modname}\n商人API：火神之心\n商人ID：{newTraderId}</b></color>";

        foreach (var (localeKey, localeKvP) in locales)
        {
            localeKvP.AddTransformer(lazyloadedLocaleData =>
            {
                lazyloadedLocaleData.Add($"{newTraderId} FullName", baseJson?.Surname);
                lazyloadedLocaleData.Add($"{newTraderId} FirstName", baseJson.Name);
                lazyloadedLocaleData.Add($"{newTraderId} Nickname", baseJson?.Nickname);
                lazyloadedLocaleData.Add($"{newTraderId} Location", baseJson?.Location);
                lazyloadedLocaleData.Add($"{newTraderId} Description", $"{baseJson.Description}{modstring}");
                return lazyloadedLocaleData;
            });
        }
    }
    

}










