using System.Text;
using System.Text.Json;
using static AssetIndex;

// @TODO support 2.8 version

// 31049740.blk
var assetIndexPath = @"I:\git\YuanShen\asset-indexes\OSREL2.7.0\release_external_asset_index.bin";
var targetPath = @"I:\git\YuanShen\asset-indexes\OSREL2.7.0\GenshinImpact_2.7.0.zip_31049740.blk.asset_index.json";

LoadIndex(assetIndexPath, targetPath);

static void LoadIndex(string assetIndexPath, string targetPath)
{
    var assetIndexStream = new FileStream(assetIndexPath, FileMode.Open);

    using (BinaryReader reader = new BinaryReader(assetIndexStream))
    {
        var typeDict = new Dictionary<string, string>();
        var subAssetDict = new Dictionary<uint, List<SubAssetInfo>>();
        var dependenciesDict = new Dictionary<uint, List<uint>>();
        var preloadBlocksList = new List<uint>();
        var preloadShaderBlocksList = new List<uint>();
        var assetsDict = new Dictionary<uint, BlockInfo>();
        var sortList = new List<uint>();

        var typeCount = reader.ReadUInt32();
        Console.WriteLine("typeCount: {0}", typeCount);

        for (int i = 0; i < typeCount; i++)
        {
            var className = mReadString(reader);
            var typeName = mReadString(reader);

            typeDict.Add(className, typeName);
            // Console.WriteLine("Type: {0} => {1}", className, typeName);
        }

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
            // Console.WriteLine("{0} {1} {2}", subAssetId, pathHashPre, pathHashLast);
        }

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

        var preloadBlocksLength = reader.ReadUInt32();
        Console.WriteLine("preloadBlocksLength: {0}", preloadBlocksLength);

        for (int i = 0; i < preloadBlocksLength; i++)
            preloadBlocksList.Add(reader.ReadUInt32());

        // Console.WriteLine(JsonSerializer.Serialize(preloadBlocksList));

        var preloadShaderBlocksLength = reader.ReadUInt32();
        Console.WriteLine("preloadShaderBlocksLength: {0}", preloadShaderBlocksLength);

        for (int i = 0; i < preloadShaderBlocksLength; i++)
            preloadShaderBlocksList.Add(reader.ReadUInt32());

        // Console.WriteLine(JsonSerializer.Serialize(preloadShaderBlocksList));

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
                assetsDict.Add(assetId, blockInfo);
                // Console.WriteLine("AssetId {0} Offset {1}", assetId, offset);
            }
        }

        var sortListLength = reader.ReadUInt32();
        Console.WriteLine("sortListLength: {0}", sortListLength);

        for (int i = 0; i < sortListLength; i++)
            sortList.Add(reader.ReadUInt32());

        // Console.WriteLine(JsonSerializer.Serialize(sortList));

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