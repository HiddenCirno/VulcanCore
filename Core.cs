using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
namespace VulcanCore;
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.hiddenhiragi.vulcancore";
    public override string Name { get; init; } = "VulcanCore";
    public override string Author { get; init; } = "HiddenHiragi";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}
[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class CorePreSptLoad(
    ISptLogger<VulcanCore> logger, DatabaseService databaseService, CustomItemService customItemService, ModHelper modHelper, JsonUtil jsonutil, ICloner cloner, ConfigServer configServer, ImageRouter imageRouter) // We inject a logger for use inside our class, it must have the class inside the diamond <> brackets
    : IOnLoad // Implement the IOnLoad interface so that this mod can do something on server load
{
    public Task OnLoad()
    {
        //new SafeRagfairPricePatch().Enable();
        //var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        VulcanUtil.DoAsyncWork(logger);
        VulcanLog.Access("test", logger);
        //LootUtils.GenerateStaticLootMap(databaseService, logger);
        //ItemUtils.GetItem("5e42c81886f7742a01529f57", databaseService).Properties.MaximumNumberOfUsage = 0; //完全可以
        //databaseService.GetTraders().Values[IEnumerable<Trader>.]
        return Task.CompletedTask;
    }
}
// We want to load after PreSptModLoader is complete, so we set our type priority to that, plus 1.
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class VulcanCore(
    ISptLogger<VulcanCore> logger,
    DatabaseService databaseService,
    CustomItemService customItemService,
    ModHelper modHelper,
    JsonUtil jsonutil,
    ICloner cloner,
    ConfigServer configServer,
    ImageRouter imageRouter,
    RagfairOfferService ragfairOfferService,
    RagfairController ragfairController,
    HandbookHelper handbookHelper
    ) // We inject a logger for use inside our class, it must have the class inside the diamond <> brackets
    : IOnLoad // Implement the IOnLoad interface so that this mod can do something on server load
{
    public Task OnLoad()
    {   
        //var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        VulcanUtil.DoAsyncWork(logger);
        VulcanLog.Access("test", logger);
        //LootUtils.GenerateStaticLootMap(databaseService, logger);
        //ItemUtils.GetItem("5e42c81886f7742a01529f57", databaseService).Properties.MaximumNumberOfUsage = 0; //完全可以
        //databaseService.GetTraders().Values[IEnumerable<Trader>.]
        return Task.CompletedTask;
    }

    public virtual GetItemPriceResult GetItemMinAvgMaxFleaPriceValues(GetMarketPriceRequestData getPriceRequest, bool ignoreTraderOffers = true)
    {
        IEnumerable<RagfairOffer> offersOfType = ragfairOfferService.GetOffersOfType(getPriceRequest.TemplateId);
        if (offersOfType!= null && offersOfType.Any())
        {
            MinMax<double> minMax = new MinMax<double>(2147483647.0, 0.0);
            var avgPriceMethod = ragfairController.GetType()
                .GetMethod("GetAveragePriceFromOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            double averagePriceFromOffers = (double)avgPriceMethod.Invoke(
                ragfairController,
                new object[] { offersOfType, minMax, ignoreTraderOffers }
            );
            //double averagePriceFromOffers = ragfairController.GetAveragePriceFromOffers(offersOfType, minMax, ignoreTraderOffers);
            return new GetItemPriceResult
            {
                Avg = Math.Round(averagePriceFromOffers),
                Min = minMax.Min,
                Max = minMax.Max
            };
        }

        if (!databaseService.GetPrices().TryGetValue(getPriceRequest.TemplateId, out var value))
        {
            value = handbookHelper.GetTemplatePrice(getPriceRequest.TemplateId);
        }
        else value = 0;

        return new GetItemPriceResult
        {
            Avg = value,
            Min = value,
            Max = value
        };
    }
}

