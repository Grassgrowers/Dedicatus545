using System.Text;
using System.Text.Json;
using static AssetIndex;

// @TODO support 2.8 version

// 31049740.blk
var assetIndexPath = @"I:\git\YuanShen\asset-indexes\OSREL2.7.0\release_external_asset_index.bin";
var targetPath = @"I:\git\YuanShen\asset-indexes\OSREL2.7.0\GenshinImpact_2.7.0.zip_31049740.blk.asset_index.json";

LoadAssetIndex(assetIndexPath, targetPath);

static void LoadAssetIndex(string assetIndexPath, string targetPath)
{
    var assetIndexStream = new FileStream(assetIndexPath, FileMode.Open);

    using (BinaryReader reader = new BinaryReader(assetIndexStream))
    {
        var typeDict = new Dictionary<string, string>();
        var subAssetDict = new Dictionary<uint, List<SubAssetInfo>>();
        var dependenciesDict = new Dictionary<uint, List<uint>>();
        var preloadBlocksList = new List<uint>();
        var preloadShaderBlocksList = new List<uint>();
        var langDict = new Dictionary<uint, uint>();
        var assetsDict = new Dictionary<uint, BlockInfo>();
        var sortList = new List<uint>();

        typeDict = LoadTypes(reader);
        subAssetDict = LoadSubAssets(reader);
        dependenciesDict = LoadDependencies(reader);
        preloadBlocksList = LoadPreloadBlocks(reader);
        preloadShaderBlocksList = LoadPreloadShaderBlocks(reader);
        langDict = LoadLangDict(reader);
        assetsDict = LoadAssets(reader, langDict);
        sortList = LoadSortList(reader);

        var assetIndex = new AssetIndex()
        {
            Types = typeDict,
            SubAssets = subAssetDict,
            Dependencies = dependenciesDict,
            PreloadBlocks = preloadBlocksList,
            PreloadShaderBlocks = preloadShaderBlocksList,
            Assets = assetsDict,
            SortList = sortList,
        };

        var jsonString = JsonSerializer.Serialize(assetIndex, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(targetPath, jsonString);
    }
}

static Dictionary<string, string> LoadTypes(BinaryReader reader)
{
    var typeDict = new Dictionary<string, string>();

    var typeCount = reader.ReadUInt32();
    Console.WriteLine("typeCount: {0}", typeCount);

    for (int i = 0; i < typeCount; i++)
    {
        var className = mReadString(reader);
        var typeName = mReadString(reader);

        typeDict.Add(className, typeName);
        // Console.WriteLine("Type: {0} => {1}", className, typeName);
    }

    return typeDict;
}

static Dictionary<uint, List<SubAssetInfo>> LoadSubAssets(BinaryReader reader)
{
    var subAssetDict = new Dictionary<uint, List<SubAssetInfo>>();

    var subAssetsCount = reader.ReadUInt32();
    Console.WriteLine("subAssetsCount: {0}", subAssetsCount);

    for (int i = 0; i < subAssetsCount; i++)
    {
        var subAssetId = reader.ReadUInt32();
        var pathHashPre = reader.ReadByte(); // Uint8
        var pathHashLast = reader.ReadUInt32();
        var unk = reader.ReadBytes(7);

        var subAssetInfo = new SubAssetInfo { Name = "", PathHashPre = pathHashPre, PathHashLast = pathHashLast };
        var subAssetList = new List<SubAssetInfo> { subAssetInfo };

        if (subAssetDict.ContainsKey(subAssetId))
            subAssetList.AddRange(subAssetDict[subAssetId]);

        subAssetDict[subAssetId] = subAssetList;
        // Console.WriteLine("subAssetId={0} pathHashPre={1} pathHashLast={2}", subAssetId, pathHashPre, pathHashLast);
    }

    return subAssetDict;
}

static Dictionary<uint, List<uint>> LoadDependencies(BinaryReader reader)
{
    var dependenciesDict = new Dictionary<uint, List<uint>>();

    var dependenciesCount = reader.ReadUInt32();
    Console.WriteLine("dependenciesCount: {0}", dependenciesCount);

    for (int i = 0; i < dependenciesCount; i++)
    {
        var dependencyKey = reader.ReadUInt32();
        var dependencyCount = reader.ReadUInt32();
        var dependencyValue = new List<uint>();

        for (int j = 0; j < dependencyCount; j++)
            dependencyValue.Add(reader.ReadUInt32());

        dependenciesDict.Add(dependencyKey, dependencyValue);
        // Console.WriteLine("{0} {1} {2}", dependencyKey, dependencyCount, JsonSerializer.Serialize(dependencyValue));
    }

    return dependenciesDict;
}

static List<uint> LoadPreloadBlocks(BinaryReader reader)
{
    var preloadBlocks = new List<uint>();

    var preloadBlocksLength = reader.ReadUInt32();
    Console.WriteLine("preloadBlocksLength: {0}", preloadBlocksLength);

    for (int i = 0; i < preloadBlocksLength; i++)
        preloadBlocks.Add(reader.ReadUInt32());

    // Console.WriteLine(JsonSerializer.Serialize(preloadBlocks));

    return preloadBlocks;
}

static List<uint> LoadPreloadShaderBlocks(BinaryReader reader)
{
    var preloadShaderBlocks = new List<uint>();

    var preloadShaderBlocksLength = reader.ReadUInt32();
    Console.WriteLine("preloadShaderBlocksLength: {0}", preloadShaderBlocksLength);

    for (int i = 0; i < preloadShaderBlocksLength; i++)
        preloadShaderBlocks.Add(reader.ReadUInt32());

    // Console.WriteLine(JsonSerializer.Serialize(preloadShaderBlocks));

    return preloadShaderBlocks;
}

static Dictionary<uint, uint> LoadLangDict(BinaryReader reader)
{
    var langDict = new Dictionary<uint, uint>(); // blockId -> langId

    var langCount = reader.ReadUInt32();
    for (int i = 0; i < langCount; i++)
    {
        var langId = reader.ReadUInt32();
        var langItemCount = reader.ReadUInt32();

        for (int j = 0; j < langItemCount; j++)
        {
            var blockId = reader.ReadUInt32();
            // AssetBundles/blocks/{langId}/{blockId}.blk

            langDict.Add(blockId, langId);
            // Console.WriteLine("langId {0} blockId {1}", langId, blockId);
        }
    }

    return langDict;
}

static Dictionary<uint, BlockInfo> LoadAssets(BinaryReader reader, Dictionary<uint, uint> langDict)
{
    var assets = new Dictionary<uint, BlockInfo>();

    var blocksCount = reader.ReadUInt32();
    Console.WriteLine("blocksCount: {0}", blocksCount);

    for (int i = 0; i < blocksCount; i++)
    {
        var blockId = reader.ReadUInt32(); // {blockId}.blk
        var blockSize = reader.ReadUInt32();

        // Console.WriteLine("BlockId {0} Size {1}", blockId, blockSize);

        for (int j = 0; j < blockSize; j++)
        {
            var assetId = reader.ReadUInt32();
            var offset = reader.ReadUInt32();

            var blockInfo = new BlockInfo { Language = langDict.ContainsKey(blockId) ? langDict[blockId] : 0, Id = blockId, Offset = offset };
            assets.Add(assetId, blockInfo);
            // Console.WriteLine("AssetId {0} Offset {1}", assetId, offset);
        }
    }

    return assets;
}

static List<uint> LoadSortList(BinaryReader reader)
{
    var sortList = new List<uint>();

    var sortListLength = reader.ReadUInt32();
    Console.WriteLine("sortListLength: {0}", sortListLength);

    for (int i = 0; i < sortListLength; i++)
        sortList.Add(reader.ReadUInt32());

    // Console.WriteLine(JsonSerializer.Serialize(sortList));

    return sortList;
}

static String mReadString(BinaryReader reader)
{
    var size = reader.ReadInt32();
    return Encoding.UTF8.GetString(reader.ReadBytes(size));
}

public class AssetIndex
{
    public Dictionary<string, string> Types { get; set; }

    public class SubAssetInfo
    {
        public string Name { get; set; }
        public byte PathHashPre { get; set; }
        public uint PathHashLast { get; set; }
    }

    public Dictionary<uint, List<SubAssetInfo>> SubAssets { get; set; }
    public Dictionary<uint, List<uint>> Dependencies { get; set; }
    public List<uint> PreloadBlocks { get; set; }
    public List<uint> PreloadShaderBlocks { get; set; }

    public class BlockInfo
    {
        public uint Language { get; set; }
        public uint Id { get; set; }
        public uint Offset { get; set; }
    }

    public Dictionary<uint, BlockInfo> Assets { get; set; }
    public List<uint> SortList { get; set; }
}