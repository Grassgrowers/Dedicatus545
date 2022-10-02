// #define EXPORT_UNMAPPED

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static AssetIndex;

// 31049740.blk
var assetIndexPath = @"I:\git\YuanShen\asset-indexes\OSCB3.1.51\release_external_asset_index.bin";
var mappedNamePath = @"I:\git\YuanShen\asset-indexes\OSCB3.1.51\mapped_name.json";
var targetPath = @"I:\git\YuanShen\asset-indexes\OSCB3.1.51\GenshinImpact_3.1.51_beta.zip_31049740.blk.asset_index.json";

LoadAssetIndex(assetIndexPath, mappedNamePath, targetPath);

static void LoadAssetIndex(string assetIndexPath, string mappedNamePath, string targetPath)
{
    var mappedName = new Dictionary<uint, Dictionary<uint, string>>();

#if !EXPORT_UNMAPPED
    using (FileStream stream = File.OpenRead(mappedNamePath))
    {
        var bytes = new byte[stream.Length];
        var count = stream.Read(bytes, 0, bytes.Length);

        if (count != bytes.Length)
            throw new Exception("Error While Reading Mapped Name");

        var jsonString = Encoding.UTF8.GetString(bytes);
        mappedName = JsonSerializer.Deserialize<Dictionary<uint, Dictionary<uint, string>>>(jsonString);
    }
#endif

    var assetIndexStream = new FileStream(assetIndexPath, FileMode.Open);

    using (BinaryReader reader = new BinaryReader(assetIndexStream))
    {
        var typeDict = new Dictionary<string, string>();
        var subAssetDict = new Dictionary<uint, List<SubAssetInfo>>();
        var dependenciesDict = new Dictionary<uint, List<uint>>();
        var preloadBlocksList = new List<uint>();
        var preloadShaderBlocksList = new List<uint>();
        var blockGroupsDict = new Dictionary<uint, uint>();
        var assetsDict = new Dictionary<uint, BlockInfo>();
        var sortList = new List<uint>();

        typeDict = LoadAssetTypeNameMap(reader);
        subAssetDict = LoadSubAssets(reader, mappedName);
        dependenciesDict = LoadBundleDependencyMap(reader);
        preloadBlocksList = LoadPreloadBlockSet(reader);
        preloadShaderBlocksList = LoadPreloadShaderBlockSet(reader);
        blockGroupsDict = LoadBlockGroups(reader);
        assetsDict = LoadAssets(reader, blockGroupsDict);
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

        var jsonString = JsonSerializer.Serialize(assetIndex, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });
        File.WriteAllText(targetPath, jsonString);
    }
}

static Dictionary<string, string> LoadAssetTypeNameMap(BinaryReader reader)
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

static Dictionary<uint, List<SubAssetInfo>> LoadSubAssets(BinaryReader reader, Dictionary<uint, Dictionary<uint, string>> mappedName)
{
    var subAssetDict = new Dictionary<uint, List<SubAssetInfo>>();
#if EXPORT_UNMAPPED
    var subAssetHashDict = new Dictionary<uint, Dictionary<uint, string>>();
#endif

    var subAssetsCount = reader.ReadUInt32();
    Console.WriteLine("subAssetsCount: {0}", subAssetsCount);

    for (int i = 0; i < subAssetsCount; i++)
    {
        var pathHashPre = reader.ReadByte(); // Uint8
        var pathHashLast = reader.ReadUInt32();
        var magic = reader.ReadBytes(5); // 00 00 00 ? 00
        var subAssetId = reader.ReadUInt32();

        if (magic[3] == 2)
            reader.ReadBytes(5);

        var name = mappedName.ContainsKey(pathHashPre) ?
            (mappedName[pathHashPre].ContainsKey(pathHashLast) ? mappedName[pathHashPre][pathHashLast] : "") : "";

        var subAssetInfo = new SubAssetInfo
        {
            Name = name,
            PathHashPre = pathHashPre,
            PathHashLast = pathHashLast
        };
        var subAssetList = new List<SubAssetInfo> { subAssetInfo };

        if (subAssetDict.ContainsKey(subAssetId))
            subAssetList.AddRange(subAssetDict[subAssetId]);

        // Console.WriteLine("subAssetId={0} pathHashPre={1} pathHashLast={2}", subAssetId, pathHashPre, pathHashLast);
        subAssetDict[subAssetId] = subAssetList;

#if EXPORT_UNMAPPED
        var hashInfo = new Dictionary<uint, string>() { { pathHashLast, "" } };

        if (subAssetHashDict.ContainsKey(pathHashPre))
            hashInfo = hashInfo.Concat(subAssetHashDict[pathHashPre]).ToDictionary(k => k.Key, v => v.Value);

        subAssetHashDict[pathHashPre] = hashInfo;
#endif
    }

#if EXPORT_UNMAPPED
    var jsonString = JsonSerializer.Serialize(subAssetHashDict, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(@"I:\git\YuanShen\asset-indexes\OSCB3.1.51\unmapped_name.json", jsonString);
    Console.WriteLine("Exported!");
    Environment.Exit(0);
#endif

    return subAssetDict;
}

static Dictionary<uint, List<uint>> LoadBundleDependencyMap(BinaryReader reader)
{
    var dependenciesDict = new Dictionary<uint, List<uint>>();

    var dependenciesCount = reader.ReadUInt32();
    reader.ReadUInt32(); // [magic] ver: 3.1.51
    Console.WriteLine("dependenciesCount: {0}", dependenciesCount);

    for (int i = 0; i < dependenciesCount; i++)
    {
        var assetId = reader.ReadUInt32();
        var dependenciesListLength = reader.ReadUInt32();
        var dependenciesList = new List<uint>();

        for (int j = 0; j < dependenciesListLength; j++)
            dependenciesList.Add(reader.ReadUInt32());

        dependenciesDict.Add(assetId, dependenciesList);
        // Console.WriteLine("assetId={0} dependenciesListLength={1} dependenciesList={2}", assetId, dependenciesListLength, JsonSerializer.Serialize(dependenciesList));
    }

    return dependenciesDict;
}

static List<uint> LoadPreloadBlockSet(BinaryReader reader)
{
    var preloadBlocks = new List<uint>();

    var preloadBlocksLength = reader.ReadUInt32();
    Console.WriteLine("preloadBlocksLength: {0}", preloadBlocksLength);

    for (int i = 0; i < preloadBlocksLength; i++)
        preloadBlocks.Add(reader.ReadUInt32());

    // Console.WriteLine(JsonSerializer.Serialize(preloadBlocks));

    return preloadBlocks;
}

static List<uint> LoadPreloadShaderBlockSet(BinaryReader reader)
{
    var preloadShaderBlocks = new List<uint>();

    var preloadShaderBlocksLength = reader.ReadUInt32();
    Console.WriteLine("preloadShaderBlocksLength: {0}", preloadShaderBlocksLength);

    for (int i = 0; i < preloadShaderBlocksLength; i++)
        preloadShaderBlocks.Add(reader.ReadUInt32());

    // Console.WriteLine(JsonSerializer.Serialize(preloadShaderBlocks));

    return preloadShaderBlocks;
}

static Dictionary<uint, uint> LoadBlockGroups(BinaryReader reader)
{
    var blockGroups = new Dictionary<uint, uint>(); // blockId -> groupId

    var blockGroupCount = reader.ReadUInt32();
    for (int i = 0; i < blockGroupCount; i++)
    {
        var groupId = reader.ReadUInt32();
        var blockCount = reader.ReadUInt32();

        for (int j = 0; j < blockCount; j++)
        {
            // AssetBundles/blocks/{groupId}/{blockId}.blk
            var blockId = reader.ReadUInt32();
            var magic = reader.ReadBytes(2); // 00 04 or 00 05

            // Console.WriteLine("groupId={0} blockId={1}", groupId, blockId);
            blockGroups.Add(blockId, groupId);
        }
    }

    return blockGroups;
}

static Dictionary<uint, BlockInfo> LoadAssets(BinaryReader reader, Dictionary<uint, uint> blockGroupsDict)
{
    var assets = new Dictionary<uint, BlockInfo>();

    var blockInfoCount = reader.ReadUInt32();
    Console.WriteLine("blockInfoCount: {0}", blockInfoCount);

    for (int i = 0; i < blockInfoCount; i++)
    {
        var blockId = reader.ReadUInt32(); // {blockId}.blk
        var assetOffsetsCount = reader.ReadUInt32();

        // Console.WriteLine("blockId={0} assetOffsetsCount={1}", blockId, assetOffsetsCount);

        for (int j = 0; j < assetOffsetsCount; j++)
        {
            var assetId = reader.ReadUInt32();
            var offset = reader.ReadUInt32();

            var blockInfo = new BlockInfo
            {
                Language = blockGroupsDict.ContainsKey(blockId) ? blockGroupsDict[blockId] : 0,
                Id = blockId,
                Offset = offset
            };
            assets.Add(assetId, blockInfo);
            // Console.WriteLine("blockId={0} assetId={1} offset={2}", blockId, assetId, offset);
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