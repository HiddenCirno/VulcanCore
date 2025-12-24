using System.Text.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;

namespace VulcanCore;

public static class VulcanUtil
{
    public static T DeepCopyJson<T>(this T obj)
    {
        if (obj == null) return default;

        // 序列化为 JSON 字符串
        var json = JsonSerializer.Serialize(obj);

        // 反序列化为新对象
        return JsonSerializer.Deserialize<T>(json);
    }

    public static void CopyNonNullProperties(object source, object target)
    {
        if (source == null || target == null)
            return;

        Type sourceType = source.GetType();
        Type targetType = target.GetType();

        foreach (var sourceProp in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var targetProp = targetType.GetProperty(sourceProp.Name);
            if (targetProp != null && targetProp.CanWrite)
            {
                var value = sourceProp.GetValue(source);
                if (value != null)
                {
                    targetProp.SetValue(target, value);
                }
            }
        }
    }
    public static bool IsHex24(string str)
    {
        if (string.IsNullOrEmpty(str))
            return false;

        return Regex.IsMatch(str, @"^[a-fA-F0-9]{24}$");
    }

    // 根据是否为Hex决定是否生成新哈希
    public static string ConvertHashID(string str)
    {
        return IsHex24(str) ? str : GenerateHash(str);
    }

    // 生成SHA1哈希并取前24位
    public static string GenerateHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha1.ComputeHash(bytes);
            string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return hex.Substring(0, 24);
        }
    }
    public static T ConvertItemData<T>(string pathToFile, string fileName, JsonUtil jsonutil)
    {
        string rawJson = File.ReadAllText(Path.Combine(pathToFile, fileName));
        JsonNode rootNode = JsonNode.Parse(rawJson);

        foreach (var item in rootNode.AsObject())
        {
            //var files = item.Value.AsValue().ToString();
            //草率了, 这里不应该用泛型定义方法返回值的....
            //就这样吧, 反正本来也是给自定义物品用的
            //再改还得改其他mod, 太麻烦了
            //论屎山是怎么形成的.jpg
            var result = ResolveJsonNode<CustomItemTemplate>(item.Value, jsonutil);
            rootNode[item.Key] = JsonNode.Parse(jsonutil.Serialize(result));
        }
        string resultJson = rootNode.ToJsonString();

        return jsonutil.Deserialize<T>(resultJson); // 返回处理后的 JsonNode

    }
    public static T ConvertItemData<T>(string file, JsonUtil jsonutil)
    {
        JsonNode rootNode = JsonNode.Parse(file).AsObject();
        return ResolveJsonNode<T>(rootNode, jsonutil); // 返回处理后的 JsonNode

    }
    public static T ResolveJsonNode<T>(JsonNode node, JsonUtil jsonUtil)
    {
        var props = node?["_props"]?.AsObject();
        if (props != null)
        {

            // Slots
            ModifySlotsOrChambers(props["Slots"]?.AsArray());

            // Chambers
            ModifySlotsOrChambers(props["Chambers"]?.AsArray());

            // Grids
            var grids = props["Grids"]?.AsArray();
            if (grids != null)
            {
                foreach (var grid in grids)
                {
                    // _parent & _id
                    if (grid?["_parent"] != null)
                        grid["_parent"] = ConvertHashID(grid["_parent"]?.GetValue<string>());
                    if (grid?["_id"] != null)
                        grid["_id"] = ConvertHashID(grid["_id"]?.GetValue<string>());

                    var filters = grid?["_props"]?["filters"]?.AsArray();
                    if (filters != null && filters.Count > 0)
                    {
                        var filterArray = filters[0]?["Filter"]?.AsArray();
                        var excludedArray = filters[0]?["ExcludedFilter"]?.AsArray();

                        if (filterArray != null)
                            for (int i = 0; i < filterArray.Count; i++)
                                filterArray[i] = ConvertHashID(filterArray[i]?.GetValue<string>());

                        if (excludedArray != null)
                            for (int i = 0; i < excludedArray.Count; i++)
                                excludedArray[i] = ConvertHashID(excludedArray[i]?.GetValue<string>());
                    }
                }
            }
            var defAmmo = props["defAmmo"];
            if (defAmmo != null)
            {
                props["defAmmo"] = ConvertHashID(defAmmo.GetValue<string>());
            }
            var FragmentType = props["FragmentType"];
            if (FragmentType != null)
            {
                props["FragmentType"] = ConvertHashID(FragmentType.GetValue<string>());
            }
            var conflict = props["ConflictingItems"];
            if (conflict != null)
            {
                var conflicts = props["ConflictingItems"]?.AsArray();
                for (int i = 0; i < conflicts.Count; i++)
                {
                    conflicts[i] = ConvertHashID(conflicts[i]?.GetValue<string>());
                }
            }
            //明天需要整理提取合并
            //sbgpt
            // StackSlots
            ModifySlotsOrChambers(props["StackSlots"]?.AsArray());
        }

        string resultJson = node.ToJsonString();

        return jsonUtil.Deserialize<T>(resultJson); // 返回处理后的 JsonNode
    }
    public static void ModifySlotsOrChambers(JsonArray array)
    {
        if (array == null) return;

        foreach (var slot in array)
        {
            // _parent & _id
            if (slot?["_parent"] != null)
                slot["_parent"] = ConvertHashID(slot["_parent"]?.GetValue<string>());
            if (slot?["_id"] != null)
                slot["_id"] = ConvertHashID(slot["_id"]?.GetValue<string>());

            var filters = slot?["_props"]?["filters"]?.AsArray();
            if (filters != null && filters.Count > 0)
            {
                var filterArray = filters[0]?["Filter"]?.AsArray();
                if (filterArray != null)
                    for (int i = 0; i < filterArray.Count; i++)
                        filterArray[i] = ConvertHashID(filterArray[i]?.GetValue<string>());
                if (filters[0]?["Plate"] != null)
                    filters[0]["Plate"] = ConvertHashID(filters[0]["Plate"].GetValue<string>());
            }
        }
    }
    public static T[] AddToArray<T>(T[] array, T item)
    {
        var list = array?.ToList() ?? new List<T>();
        list.Add(item);
        return list.ToArray();
    }
    public static async Task DoAsyncWork(ISptLogger<VulcanCore> logger)
    {
        await Task.Run(() =>
        {
            // 这里就是异步执行的内容
            VulcanLog.Log("test123", logger);
        });
    }
    public static T ConvertTraderBaseData<T>(string pathToFile, string fileName, JsonUtil jsonutil)
    {
        // 读取原始 JSON
        string rawJson = File.ReadAllText(Path.Combine(pathToFile, fileName));

        // 解析 JSON
        JsonNode rootNode = JsonNode.Parse(rawJson);

        // 确保是对象
        if (rootNode is JsonObject objNode)
        {
            // 修改 _id
            if (objNode["_id"] != null)
            {
                objNode["_id"] = ConvertHashID(objNode["_id"].GetValue<string>());
            }
        }
        else
        {
            //logger.LogError($"ConvertTraderBaseData: JSON 根节点不是对象 -> {fileName}");
            throw new InvalidOperationException("JSON 根节点必须是对象");
        }

        // 转换回 JSON 字符串
        string resultJson = rootNode.ToJsonString();
        //VulcanLog.Log(resultJson, logger);

        // 反序列化返回
        return jsonutil.Deserialize<T>(resultJson);
    }
    public class MongoIdConverter : JsonConverter<MongoId>
    {
        public override MongoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string str = reader.GetString()!;
            return (MongoId)VulcanUtil.ConvertHashID(str);
        }

        public override void Write(Utf8JsonWriter writer, MongoId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
    public static T DrawFromList<T>(List<T> list)
    {
        if (list.Count == 0)
        {
            throw new ArgumentException("列表为空", nameof(list));
        }

        Random random = new Random();
        int randomIndex = random.Next(list.Count);  // 生成一个随机索引
        return list[randomIndex];
    }
    public static string DoubleToPercent(double num)
    {
        // 检查输入是否为有效数字
        if (double.IsNaN(num))
        {
            return "NaN";
        }

        // 将浮点数乘以100转换为百分比，然后保留两位小数
        var percent = (num * 100).ToString("F3");

        // 添加百分号返回
        return percent + "%";
    }
}





