using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Newtonsoft.Json.Linq;

namespace JetIslandArchipelago;

public partial class ArchipelagoWrapper
{
    private const ItemsHandlingFlags ItemFlags = ItemsHandlingFlags.AllItems;
    private static readonly Version Version = new(6,1,0);

    private static ArchipelagoWrapper _instance;
    public static ArchipelagoWrapper Instance => _instance ??= new ArchipelagoWrapper(); 
    private ArchipelagoSession _session;

    public delegate void DeathReceivedHandler(string cause, string source, DateTime time);
    public event DeathReceivedHandler OnDeathReceived;
    public delegate void DisconnectedHandler();
    public event DisconnectedHandler OnDisconnected;
    public delegate void ConnectHandler(int slotNumber, int teamNumber, Dictionary<string,object> slotData);
    public event ConnectHandler OnConnect;
    public delegate void MessageHandler(string richText);
    public event MessageHandler OnMessage;
    public delegate void ReceiveItem(long id, string name,bool newItem);
    public event ReceiveItem OnReceiveItem;
    public event Action<long,CheckType,int> OnCheck;

    private static DeathLinkService _deathLinkService;
    static bool _deathLinkEnabled;
    public static bool DeathLinkEnabled
    {
        get => _deathLinkEnabled;
        set
        {
            if (value == _deathLinkEnabled)
                return;
            _deathLinkEnabled = value;
            
            if(value)
                _deathLinkService.EnableDeathLink();
            else
                _deathLinkService.DisableDeathLink();
            
        }
    }

    private static void OnDeathLink(DeathLink link)
    {
        if(_deathLinkEnabled)
            Instance.OnDeathReceived?.Invoke(link.Cause, link.Source, link.Timestamp);
    }

    public void SendDeathLink(string cause)
    {
        if(_deathLinkEnabled)
            _deathLinkService.SendDeathLink(new DeathLink(_slotName,cause));
    }

    private ArchipelagoWrapper()
    {
        OnCheck += (id, type, index) =>
        {
            CheckedLocations.Add(id, (type, index));
        };
        OnDisconnected += () =>
        {
            CheckedLocations.Clear();
            ReceivedItems.Clear();
        };
    }
    
    public string Host { get; private set; }
    public string Slot { get; private set; }
    static void OnReceiveMessage(LogMessage msg)
    {
        Instance.OnMessage?.Invoke(string.Join("",msg.Parts.Select(GetMessage)));
        return;

        static string GetMessage(MessagePart part)
        {
            return $"<{(part.IsBackgroundColor ? "mark" : "color")}=#{part.Color.R:X2}{part.Color.G:X2}{part.Color.B:X2}>{part.Text}</color>";
        }
    }

    private string _slotName;
    public bool Connected { get; private set; }
    public bool Connecting { get; private set; }

    public async Task<bool> Connect(string host, string slot, bool deathlink, string password)
    {
        Connected = false;
        Connecting = true;
        if (_session != null)
        {
            try{
                await _session.Socket.DisconnectAsync();
            }
            catch
            {
                // ignored
            }

            _session = null;
        }

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(new Uri(host));
            _session.Socket.SocketClosed += OnSocketClose;
            _session.Socket.ErrorReceived += OnSocketError;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
            Connecting = false;
            OnDisconnected?.Invoke();
            return false;
        }

        try
        {
            var roomInfo = await _session.ConnectAsync();
            if (roomInfo.Password && string.IsNullOrEmpty(password))
            {
                Plugin.Logger.LogError("Room Requires Password");
                Connecting = false;
                OnDisconnected?.Invoke();
                return false;

            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
            Connecting = false;
            OnDisconnected?.Invoke();
            return false;
        }

        try
        {
            var result = await _session.LoginAsync(GameName, slot, ItemFlags, Version, [],
                null, password);
            if (!result.Successful)
            {
                var failure = (LoginFailure)result;
                Plugin.Logger.LogError($"Login failed:\n{string.Join("\n\t",failure.Errors)}");
                Connecting = false;
                OnDisconnected?.Invoke();
                return false;
            }
            _slotName = slot;
            _session.MessageLog.OnMessageReceived += OnReceiveMessage;
            _deathLinkService = _session.CreateDeathLinkService();
            _deathLinkService.OnDeathLinkReceived += OnDeathLink;
            
            Connected = true;
            Connecting = false;
            Host = host;
            Slot = slot;
            var success = (LoginSuccessful)result;
            _baseId = (long)success.SlotData["base_id"];
            OnConnect?.Invoke(success.Slot,success.Team,success.SlotData);
            SendChecks();
            _session.Items.ItemReceived += helper =>
            {
                var item = helper.DequeueItem();
                if (ReceivedItems.ContainsKey(item.ItemId))
                    return;
                ReceivedItems.Add(item.ItemId, (item.ItemId, item.ItemName));
                OnReceiveItem?.Invoke(item.ItemId,item.ItemName,true);
                
            };
            while (_session.Items.DequeueItem() is { } item)
            {
                if (ReceivedItems.ContainsKey(item.ItemId))
                    continue;
                ReceivedItems.Add(item.ItemId, (item.ItemId, item.ItemName));
                OnReceiveItem?.Invoke(item.ItemId, item.ItemName,false);
            }

            _deathLinkEnabled = deathlink;
            if(deathlink)
                _deathLinkService.EnableDeathLink();
            
            return true;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e.ToString());
            Connecting = false;
            OnDisconnected?.Invoke();
            return false;
        }
    }

    public async Task Disconnect()
    {
        Connected = false;
        await _session?.Socket?.DisconnectAsync()!;
        _session = null;
        OnDisconnected?.Invoke();
    }

    private static void OnSocketClose(string reason)
    {
        Plugin.Logger.LogError($"Socket closed: {reason}");
    }

    private static void OnSocketError(Exception e, string reason)
    {
        Plugin.Logger.LogError($"{reason}: {e}");
    }

    private long _baseId;
    public void Say(string message) =>  _session.Say(message);
    

    public struct LocationInfo
    {
        public int AllLocations;
        public int LocationsChecked;
        public int LocationsUnchecked;
    }

    public LocationInfo GetLocationInfo()
    {
        return new LocationInfo()
        {
            AllLocations = _session.Locations.AllLocations.Count,
            LocationsChecked = _session.Locations.AllLocationsChecked.Count,
            LocationsUnchecked = _session.Locations.AllMissingLocations.Count
        };
    }

    public ReadOnlyCollection<long> GetAllLocations() => _session.Locations.AllLocations;
    public ReadOnlyCollection<long> GetCheckedLocations() => _session.Locations.AllLocationsChecked;
    public ReadOnlyCollection<long> GetMissingLocations() => _session.Locations.AllMissingLocations;
    
    public string GetLocationName(long id) => _session.Locations.GetLocationNameFromId(id);

    public T GetDataStorage<T>(string key, T defaultValue)
    {
        _session.DataStorage[Scope.Slot,key].Initialize(JToken.FromObject(defaultValue));
        var ret =  _session.DataStorage[Scope.Slot, key].To<T>();
        Plugin.Logger.LogDebug($"Getting data storage[{key}]: {ret}");
        return ret;
    }
    public void SetDataStorage<T>(string key, T value)
    {
        Plugin.Logger.LogDebug($"Setting data storage[{key}]: {value}");   
        _session.DataStorage[Scope.Slot, key] = JToken.FromObject(value);
    }


    public async Task Check(long id)
    {
        await _session.Locations.CompleteLocationChecksAsync(id);
    }

    public void Release()
    {
        _session.SetGoalAchieved();
    }
    
}
