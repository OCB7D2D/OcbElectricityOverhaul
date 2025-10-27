using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ####################################################################
// This file is a verbatim copy of the decompiled original class.
// We only changed certain accessor types, e.g. made them public.
// Implement changes/adjustements in the specialized class.
// This should allow us to update vanilla code more easily.
// ####################################################################

public class PowerManagerBase
{

    // ####################################################################
    // ####################################################################

    private bool Loaded = false; // OCB Patched
    private const float UPDATE_TIME_SEC = 0.16f;
    private const float SAVE_TIME_SEC = 120f;
    public static byte FileVersion = 2;
    private static PowerManagerBase instance;
    protected List<PowerItem> Circuits;
    protected List<OcbPowerSource> PowerSources;
    protected List<PowerTrigger> PowerTriggers;
    public Dictionary<Vector3i, PowerItem> PowerItemDictionary = new Dictionary<Vector3i, PowerItem>();
    protected float updateTime;
    protected float saveTime = 120f;
    protected ThreadManager.ThreadInfo dataSaveThreadInfo;
    public List<TileEntityPoweredBlock> ClientUpdateList = new List<TileEntityPoweredBlock>();

    // ####################################################################
    // ####################################################################

    public byte CurrentFileVersion { get; set; }

    // ####################################################################
    // ####################################################################

    public static PowerManagerBase Instance
    {
        get
        {
            if (PowerManagerBase.instance == null)
                PowerManagerBase.instance = new OcbPowerManager();
            return PowerManagerBase.instance;
        }
    }

    public static bool HasInstance => PowerManagerBase.instance != null;

    // ####################################################################
    // ####################################################################

    public PowerManagerBase()
    {
        PowerManagerBase.instance = this;
        this.Circuits = new List<PowerItem>();
        this.PowerSources = new List<OcbPowerSource>();
        this.PowerTriggers = new List<PowerTrigger>();
        this.Loaded = false; // OCB Patched
    }

    // ####################################################################
    // ####################################################################

    public virtual void Update()
    {
        if (GameManager.Instance.World == null || GameManager.Instance.World.Players == null || GameManager.Instance.World.Players.Count == 0)
            return;
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer && GameManager.Instance.gameStateManager.IsGameStarted())
        {
            this.updateTime -= Time.deltaTime;
            if ((double)this.updateTime <= 0.0)
            {
                for (int index = 0; index < this.PowerSources.Count; ++index)
                    this.PowerSources[index].Update();
                for (int index = 0; index < this.PowerTriggers.Count; ++index)
                    this.PowerTriggers[index].CachedUpdateCall();
                this.updateTime = 0.16f;
            }
            this.saveTime -= Time.deltaTime;
            if ((double)this.saveTime <= 0.0 && (this.dataSaveThreadInfo == null || this.dataSaveThreadInfo.HasTerminated()))
            {
                this.saveTime = 120f;
                this.SavePowerManager();
            }
        }
        for (int index = 0; index < this.ClientUpdateList.Count; ++index)
            this.ClientUpdateList[index].ClientUpdate();
    }

    // ####################################################################
    // ####################################################################

    private int savePowerDataThreaded(ThreadManager.ThreadInfo _threadInfo)
    {
        PooledExpandableMemoryStream parameter = (PooledExpandableMemoryStream)_threadInfo.parameter;
        string str = string.Format("{0}/{1}", (object)GameIO.GetSaveGameDir(), (object)"power.dat");
        if (SdFile.Exists(str))
            SdFile.Copy(str, string.Format("{0}/{1}", (object)GameIO.GetSaveGameDir(), (object)"power.dat.bak"), true);
        parameter.Position = 0L;
        StreamUtils.WriteStreamToFile((Stream)parameter, str);
        MemoryPools.poolMemoryStream.FreeSync(parameter);
        return -1;
    }

    // ####################################################################
    // ####################################################################

    public virtual void LoadPowerManager()
    {
        string path1 = string.Format("{0}/{1}", (object)GameIO.GetSaveGameDir(), (object)"power.dat");
        if (!SdFile.Exists(path1))
        {
            // OCB Patched
            Loaded = true;
            return;
        }
        try
        {
            using (Stream _stream = SdFile.OpenRead(path1))
            {
                using (PooledBinaryReader br = MemoryPools.poolBinaryReader.AllocSync(false))
                {
                    br.SetBaseStream(_stream);
                    this.Read((BinaryReader)br);
                }
            }
        }
        catch (Exception err)
        {
            Log.Warning("Failed to read Power Manager Save State");
            Log.Warning("Falling back to read previous backup file");
            Log.Warning("Error: {0}", err);
            string path2 = string.Format("{0}/{1}", (object)GameIO.GetSaveGameDir(), (object)"power.dat.bak");
            if (!SdFile.Exists(path2))
            {
                // OCB Patched
                Loaded = true;
                return;
            }
            using (Stream _stream = SdFile.OpenRead(path2))
            {
                using (PooledBinaryReader br = MemoryPools.poolBinaryReader.AllocSync(false))
                {
                    br.SetBaseStream(_stream);
                    this.Read((BinaryReader)br);
                }
            }
        }
    }

    // ####################################################################
    // ####################################################################

    public void SavePowerManager()
    {
        if (Loaded == false) return; // OCB Patched
        if (this.dataSaveThreadInfo != null && ThreadManager.ActiveThreads.ContainsKey("silent_powerDataSave"))
            return;
        PooledExpandableMemoryStream expandableMemoryStream = MemoryPools.poolMemoryStream.AllocSync(true);
        using (PooledBinaryWriter bw = MemoryPools.poolBinaryWriter.AllocSync(false))
        {
            bw.SetBaseStream((Stream)expandableMemoryStream);
            this.Write((BinaryWriter)bw);
        }
        this.dataSaveThreadInfo = ThreadManager.StartThread("silent_powerDataSave", (ThreadManager.ThreadFunctionDelegate)null, new ThreadManager.ThreadFunctionLoopDelegate(this.savePowerDataThreaded), (ThreadManager.ThreadFunctionEndDelegate)null, _parameter: ((object)expandableMemoryStream));
    }

    // ####################################################################
    // ####################################################################

    public virtual void Write(BinaryWriter bw)
    {
        bw.Write(PowerManagerBase.FileVersion);
        bw.Write(this.Circuits.Count);
        for (int index = 0; index < this.Circuits.Count; ++index)
        {
            bw.Write((byte)this.Circuits[index].PowerItemType);
            this.Circuits[index].write(bw);
        }
    }

    public virtual void Read(BinaryReader br) => Read(br, br.ReadByte());

    public void Read(BinaryReader br, byte header)
    {
        // Also set legacy FileVersion (play safe)
        PowerManager.FileVersion = header;
        this.CurrentFileVersion = header;
        this.Circuits.Clear();
        int num = br.ReadInt32();
        for (int index = 0; index < num; ++index)
        {
            PowerItem node = PowerItem.CreateItem((PowerItem.PowerItemTypes)br.ReadByte());
            node.read(br, this.CurrentFileVersion);
            this.AddPowerNode(node);
        }
        // OCB Patched
        this.Loaded = true;
    }

    // ####################################################################
    // ####################################################################

    public void Cleanup()
    {
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            this.SavePowerManager();
        PowerManagerBase.instance = (PowerManagerBase)null;
        this.Circuits.Clear();
        if (this.dataSaveThreadInfo == null)
            return;
        this.dataSaveThreadInfo.WaitForEnd();
        this.dataSaveThreadInfo = (ThreadManager.ThreadInfo)null;
    }

    // ####################################################################
    // ####################################################################

    public void AddPowerNode(PowerItem node, PowerItem parent = null)
    {
        this.Circuits.Add(node);
        this.SetParent(node, parent);
        if (node is OcbPowerSource)
            this.PowerSources.Add((OcbPowerSource)node);
        if (node is PowerTrigger)
            this.PowerTriggers.Add((PowerTrigger)node);
        this.PowerItemDictionary.Add(node.Position, node);
    }

    public void RemovePowerNode(PowerItem node)
    {
        foreach (PowerItem child in new List<PowerItem>((IEnumerable<PowerItem>)node.Children))
            this.SetParent(child, (PowerItem)null);
        this.SetParent(node, (PowerItem)null);
        this.Circuits.Remove(node);
        if (node is OcbPowerSource)
            this.PowerSources.Remove((OcbPowerSource)node);
        if (node is PowerTrigger)
            this.PowerTriggers.Remove((PowerTrigger)node);
        if (!this.PowerItemDictionary.ContainsKey(node.Position))
            return;
        this.PowerItemDictionary.Remove(node.Position);
    }

    public void RemoveUnloadedPowerNodes(ICollection<long> _chunks)
    {
        int num1 = 0;
        int count = this.PowerItemDictionary.Count;
        List<Vector3i> span = new List<Vector3i>(count);
        foreach (KeyValuePair<Vector3i, PowerItem> powerItem in this.PowerItemDictionary)
        {
            long num2 = WorldChunkCache.MakeChunkKey(World.toChunkXZ(powerItem.Key.x), World.toChunkXZ(powerItem.Key.z));
            foreach (long chunk in (IEnumerable<long>)_chunks)
            {
                if (chunk == num2)
                    span[num1++] = powerItem.Key;
            }
        }
        for (int index = 0; index < num1; ++index)
        {
            PowerItem node;
            if (this.PowerItemDictionary.TryGetValue(span[index], out node))
                this.RemovePowerNode(node);
        }
    }

    // ####################################################################
    // ####################################################################

    public virtual void SetParent(PowerItem child, PowerItem parent)
    {
        if (child == null || child.Parent == parent || this.CircularParentCheck(parent, child))
            return;
        if (child.Parent != null)
            this.RemoveParent(child);
        if (parent == null)
            return;
        if (child != null && this.Circuits.Contains(child))
            this.Circuits.Remove(child);
        parent.Children.Add(child);
        child.Parent = parent;
        child.SendHasLocalChangesToRoot();
    }

    private bool CircularParentCheck(PowerItem Parent, PowerItem Child)
    {
        if (Parent == Child)
            return true;
        return Parent != null && Parent.Parent != null && this.CircularParentCheck(Parent.Parent, Child);
    }

    public virtual void RemoveParent(PowerItem node)
    {
        if (node.Parent == null)
            return;
        PowerItem parent = node.Parent;
        node.Parent.Children.Remove(node);
        if (node.Parent.TileEntity != null)
        {
            node.Parent.TileEntity.CreateWireDataFromPowerItem();
            node.Parent.TileEntity.DrawWires();
        }
        node.Parent = (PowerItem)null;
        this.Circuits.Add(node);
        parent.SendHasLocalChangesToRoot();
        node.HandleDisconnect();
    }

    // ####################################################################
    // ####################################################################

    public void RemoveChild(PowerItem child)
    {
        child.Parent.Children.Remove(child);
        child.Parent = (PowerItem)null;
        this.Circuits.Add(child);
    }

    public void SetParent(Vector3i childPos, Vector3i parentPos)
    {
        PowerItem powerItemByWorldPos = this.GetPowerItemByWorldPos(parentPos);
        this.SetParent(this.GetPowerItemByWorldPos(childPos), powerItemByWorldPos);
    }

    // ####################################################################
    // ####################################################################

    public PowerItem GetPowerItemByWorldPos(Vector3i position) => PowerItemDictionary
        .ContainsKey(position) ? this.PowerItemDictionary[position] : (PowerItem)null;

    // ####################################################################
    // ####################################################################

    public void LogPowerManager()
    {
        for (int index = 0; index < this.PowerSources.Count; ++index)
            this.LogChildren((PowerItem)this.PowerSources[index]);
    }

    private void LogChildren(PowerItem item)
    {
        try
        {
            Log.Out(string.Format("{0}{1}({2}) - Pos:{3} | Powered:{4}", (object)new string('\t',
                item.Depth > (ushort)100 ? 0 : (int)item.Depth + 1), (object)item.ToString(),
                (object)item.Depth, (object)item.Position, (object)item.IsPowered));
            for (int index = 0; index < item.Children.Count; ++index)
                this.LogChildren(item.Children[index]);
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
        }
    }

    // ####################################################################
    // ####################################################################

}

