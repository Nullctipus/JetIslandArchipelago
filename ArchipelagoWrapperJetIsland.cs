using System.Collections.Generic;
using System.Threading.Tasks;

namespace JetIslandArchipelago;

public partial class ArchipelagoWrapper
{
    private const string GameName = "Jet Island";
    
    private const int ModifierOffset = 4;
    private const int BossOffset = 37;
    private const int FillerOffset = 42;
    private const int ModifierLocationOffset = 20;
    private const int BossLocationOffset = 54;
    public static readonly Dictionary<long, (long, string)> ReceivedItems = [];
    public static readonly Dictionary<long, (CheckType, int)> CheckedLocations = [];

    public struct ItemInfo
    {
        public enum ItemType
        {
            Upgrade,
            Modifier,
            BossUpgrade,
            Filler
        }

        public ItemType Type;
        public int Index;
    }

    public ItemInfo GetItemInfo(long id)
    {
        return (id - _baseId) switch
        {
            < ModifierOffset => new ItemInfo
            {
                Type = ItemInfo.ItemType.Upgrade,
                Index = (int)(id - _baseId),
            },
            < BossOffset => new ItemInfo()
            {
                Type = ItemInfo.ItemType.Modifier,
                Index = (int)((id - _baseId - ModifierOffset) >= 17
                    ? id - _baseId - ModifierOffset + 1
                    : id - _baseId - ModifierOffset),
            },
            < FillerOffset => new ItemInfo()
            {
                Type = ItemInfo.ItemType.BossUpgrade,
                Index = (int)(id - _baseId - BossOffset),
            },
            _ => new ItemInfo()
            {
                Type = ItemInfo.ItemType.Filler,
                Index = (int)(id - _baseId - FillerOffset),
            }
        };
    }

    public enum CheckType
    {
        Stat,
        Modifier,
        Boss,
    }

    public async Task CheckUpgradeBot(int index)
    {
        Plugin.Logger.LogDebug($"Checking bot {index}");
        OnCheck?.Invoke(_baseId + index, CheckType.Stat, index);
        await Check(_baseId + index);
    }

    public async Task CheckModifier(int index)
    {
        Plugin.Logger.LogDebug($"Checking modifier {index}");
        OnCheck?.Invoke(_baseId + ModifierLocationOffset + index, CheckType.Modifier, index);
        await Check(_baseId + ModifierLocationOffset + index);
    }

    public async Task CheckMiniBoss(int index)
    {
        Plugin.Logger.LogDebug($"Checking miniboss {index}");
        OnCheck?.Invoke(_baseId + BossLocationOffset + index, CheckType.Boss, index);
        await Check(_baseId + BossLocationOffset + index);
    }

    public void SendChecks()
    {
        foreach (var id in _session.Locations.AllLocationsChecked)
        {
            int lid = (int)(id - _baseId);
            switch (id-_baseId)
            {
                case < ModifierLocationOffset:
                    OnCheck?.Invoke(id,CheckType.Stat,lid);
                    break;
                case < BossLocationOffset:
                    OnCheck?.Invoke(id,CheckType.Modifier,lid-ModifierLocationOffset);
                    break;
                default:
                    OnCheck?.Invoke(id,CheckType.Boss,lid-BossLocationOffset);
                    break;
            }
        }
    }
}