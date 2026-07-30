using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using TupleJammingEntry = VRage.MyTuple<VRageMath.Vector3, float, long>;
using TupleJammingEntryV2 = VRage.MyTuple<VRageMath.Vector3D, float, long, VRageMath.Vector3D, long>;
using TupleJammingEntryPb = VRage.MyTuple<VRageMath.Vector3D, float>;
using TupleRadarEntry = VRage.MyTuple<uint, string, VRage.MyTuple<VRageMath.Vector3, VRageMath.Vector3, float, float>, long, byte>;
using TupleRadarEntryV2 = VRage.MyTuple<ushort, string, VRage.MyTuple<VRageMath.Vector3D, VRageMath.Vector3, float, float>, long, byte, byte>;
using TupleRadarEntryPb = VRage.MyTuple<ushort, string, VRage.MyTuple<VRageMath.Vector3D, VRageMath.Vector3, float, float>, long, byte, byte>;
using TupleRadarPositionData = VRage.MyTuple<VRageMath.Vector3, VRageMath.Vector3, float, float>;
using TupleRadarPositionDataV2 = VRage.MyTuple<VRageMath.Vector3D, VRageMath.Vector3, float, float>;
using TupleRadarPositionDataPb = VRage.MyTuple<VRageMath.Vector3D, VRageMath.Vector3, float, float>;
using TuplePassiveRadarEntry = VRage.MyTuple<VRage.MyTuple<byte, ushort>, float, float, long, VRageMath.Vector3D, VRageMath.Vector3D>;
using TuplePassiveRadarEntryPb = VRage.MyTuple<byte, float, float, long, VRageMath.Vector3D, VRageMath.Vector3D>;
using VRage.Game.Entity;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace NebRadarAPI.API
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class NebRadarAPI : MySessionComponentBase
    {
        public const string ModAPIVersion = "v2.1";
        public const long ModAPIMessageID = 3290983434;

        /// <summary>
        /// Call during LoadData; main mod is blind and will only send API functions ONCE on Init.
        /// </summary>
        /// <param name="callback"></param>
        public static void Load(Action callback = null)
        {
            MyAPIUtilities.Static.RegisterMessageHandler(ModAPIMessageID, OnModMessageRecieved);
            OnAPILoad = callback;
        }
        /// <summary>
        /// Call during UnloadData or world reloads may cause crashes.
        /// </summary>
        protected override void UnloadData()
        {
            MyAPIUtilities.Static.UnregisterMessageHandler(ModAPIMessageID, OnModMessageRecieved);

            OnAPILoad = null;
            _canSee = null;
        }
        /// <summary>
        /// True when API contains the proper delegates, false otherwise.
        /// </summary>
        public static bool IsReady
        {
            get; private set;
        }
        

        public enum Relation : byte
        {
            Neutral,
            Enemy,
            Allied
        }

        [Flags]
        public enum StateFlags : byte
        {
            None = 0,
            /// <summary>
            /// Set if the entry is a jump signature
            /// </summary>
            Jumping = 1 << 0,
            /// <summary>
            /// Only used by networking, ignore
            /// </summary>
            Delete = 1 << 7,
        }

        [Flags]
        public enum PassiveStateFlags : byte
        {
            None = 0,
            /// <summary>
            /// set if the passive radar entry is a crossfix
            /// </summary>
            Position = 1,
            /// <summary>
            /// Only used by networking, ignore
            /// </summary>
            Delete = 1 << 7
        }
        public struct RadarEntry
        {
            public Relation relation;
            public StateFlags RadarFlags;
            public ushort TrackNumber;
            public float PositionError;
            public float VelocityError;
            public string Name;
            public long MainGridEntityId;
            /// <summary>
            /// Exact
            /// </summary>
            public Vector3D Position;
            /// <summary>
            /// Exact
            /// </summary>
            public Vector3 Velocity;
            public bool IsLocked => PositionError + VelocityError == 0;

            public RadarEntry(TupleRadarEntryV2 data)
            {
                TrackNumber = data.Item1;
                Name = data.Item2;
                Position = data.Item3.Item1;
                Velocity = data.Item3.Item2;
                PositionError = data.Item3.Item3;
                VelocityError = data.Item3.Item4;
                MainGridEntityId = data.Item4;
                relation = (Relation)data.Item5;
                RadarFlags = (StateFlags)data.Item6;
            }
        }
        public struct JammingEntry
        {
            public float AreaEffectRatio;
            public long JammerEntityId;
            public long TargetEntityId;
            /// <summary>
            /// Exact
            /// </summary>
            public Vector3D Position;
            public Vector3D TargetPos;

            public JammingEntry(TupleJammingEntryV2 data)
            {
                Position = data.Item1;
                AreaEffectRatio = data.Item2;
                JammerEntityId = data.Item3;
                TargetPos = data.Item4;
                TargetEntityId = data.Item5;
            }
        }
        // This does two different things mainly because its copied over from networking
        public struct PassiveSensorEntry
        {
            public PassiveStateFlags RadarFlags;
            /// <summary>
            /// Unused if position
            /// </summary>
            public ushort TargetTrackNum;
            /// <summary>
            ///  Radians or meters
            /// </summary>
            public float Error;
            /// <summary>
            /// Highest SNR if position, SNR from the sensor if direction
            /// </summary>
            public float Power;
            /// <summary>
            /// Sensor block entity ID if direction, target grid entity ID if positon
            /// </summary>
            public long EntityId;
            /// <summary>
            /// Position if position, direction normalized if direction
            /// </summary>
            public Vector3D Vector;
            /// <summary>
            /// Sensor position if direction, zero otherwise
            /// </summary>
            public Vector3D SensorPosition;

            public bool Direction => !RadarFlags.HasFlag(PassiveStateFlags.Position);
            public bool Position => RadarFlags.HasFlag(PassiveStateFlags.Position);
            public PassiveSensorEntry(TuplePassiveRadarEntry data)
            {
                RadarFlags = (PassiveStateFlags)data.Item1.Item1;
                TargetTrackNum = data.Item1.Item2;
                Error = data.Item2;
                Power = data.Item3;
                EntityId = data.Item4;
                Vector = data.Item5;
                SensorPosition = data.Item6;
            }
        }
        /// <summary>
        /// Only callable on server. <br/>
        /// Returns whether the <paramref name="radarGrid"/> can see the <paramref name="target"/> grid. <br/>
        /// Laggy!<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="radarGrid">Grid seeing</param>
        /// <param name="target">Grid potentially seen</param>
        /// <returns></returns>
        public static bool CanSee(IMyCubeGrid radarGrid, IMyCubeGrid target)
        {
            return _canSee.Invoke(radarGrid, target);
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets whether the given <paramref name="grid"/> is seen by any neutral or hostile radar.<br/>
        /// </summary>
        /// <param name="grid">Grid to check, includes subgrids in the check</param>
        /// <returns></returns>
        public static bool IsGridSeen(IMyCubeGrid grid)
        {
            return _isGridSeen.Invoke(grid);
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets whether the given <paramref name="grid"/> is locked by any neutral or hostile radar.<br/>
        /// </summary>
        /// <param name="grid">Grid to check, includes subgrids in the check</param>
        /// <returns></returns>
        public static bool IsGridLocked(IMyCubeGrid grid)
        {
            return _isGridLocked.Invoke(grid);
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all active entries from all active radars on <paramref name="grid"/> and from the grid's subgrids and datalink, and populates results in <paramref name="returnList"/>.<br/>
        /// Laggy!<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="grid">Grid to get entries from. Includes subgrids</param>
        /// <returns></returns>
        public static void GetAllRadarEntries(IMyCubeGrid grid, List<RadarEntry> returnList)
        {
            _getAllRadarEntries.Invoke(grid, _TupleRadarEntryDumpList);
            foreach (var entry in _TupleRadarEntryDumpList)
            {
                returnList.Add(new RadarEntry(entry));
            }
            _TupleRadarEntryDumpList.Clear();
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all active active radar entries visible to the given <paramref name="radar"/>, and populates results in <paramref name="returnList"/>.<br/>
        /// Will return an empty list if the block is not a radar block or is off. <br/>
        /// Laggy! (but less than <see cref="GetAllRadarEntries(IMyCubeGrid, List{RadarEntry})"/>)<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="radar">Block to get entries from.</param>
        /// <param name="returnList">List to recieve the return values from to reduce alloc</param>
        /// <returns></returns>
        public static void GetAllRadarEntries(IMyFunctionalBlock radar, List<RadarEntry> returnList)
        {
            _getAllRadarEntriesRadar.Invoke(radar, _TupleRadarEntryDumpList);
            foreach (var entry in _TupleRadarEntryDumpList)
            {
                returnList.Add(new RadarEntry(entry));
            }
            _TupleRadarEntryDumpList.Clear();
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all jamming entries visible to the given <paramref name="grid"/>, and populates results in <paramref name="returnList"/>.<br/>
        /// Laggy!<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="radar">Block to get entries from.</param>
        /// <param name="returnList">List to recieve the return values from to reduce alloc</param>
        /// <returns></returns>
        public static void GetAllJammingEntries(IMyCubeGrid grid, List<JammingEntry> returnList)
        {
            _getAllJammingEntries.Invoke(grid, _TupleJammingEntryV2DumpList);
            foreach (var entry in _TupleJammingEntryV2DumpList)
            {
                returnList.Add(new JammingEntry(entry));
            }
            _TupleJammingEntryV2DumpList.Clear();
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all jamming entries visible to the given <paramref name="radar"/> block, and populates results in <paramref name="returnList"/>.<br/>
        /// Will return an empty list if the block is not a radar block or is off. <br/>
        /// Laggy! (But less than <see cref="GetAllJammingEntries(IMyCubeGrid, List{JammingEntry})"/>)<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="radar">Block to get entries from.</param>
        /// <param name="returnList">List to recieve the return values from to reduce alloc</param>
        /// <returns></returns>
        public static void GetAllJammingEntries(IMyFunctionalBlock radar, List<JammingEntry> returnList)
        {
            _getAllJammingEntriesRadar.Invoke(radar, _TupleJammingEntryV2DumpList);
            foreach (var entry in _TupleJammingEntryV2DumpList)
            {
                returnList.Add(new JammingEntry(entry));
            }
            _TupleJammingEntryV2DumpList.Clear();
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all active radar blocks on the given <paramref name="grid"/>, and populates results in <paramref name="returnList"/>.<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="grid">Grid to get blocks from. Includes subgrids</param>
        /// <param name="returnList">List to recieve the return values from to reduce alloc</param>
        /// <returns></returns>
        public static void GetAllRadarBlocks(IMyCubeGrid grid, List<IMyFunctionalBlock> returnList)
        {
            _getAllRadarBlocks.Invoke(grid, returnList);
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all upgrade blocks on the given <paramref name="grid"/>, and populates results in <paramref name="returnList"/>. <br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="grid">Grid to get blocks from. Includes subgrids</param>
        /// <param name="returnList">List to recieve the return values from to reduce alloc</param>
        /// <returns></returns>
        public static void GetAllUpgradeBlocks(IMyCubeGrid grid, List<IMyTerminalBlock> returnList)
        {
            _getAllUpgradeBlocks.Invoke(grid, returnList);
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all passive radar blocks on the given <paramref name="grid"/>, and populates results in <paramref name="returnList"/>.<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="grid">Grid to get blocks from. Includes subgrids</param>
        /// <param name="returnList">List to recieve the return values from to reduce alloc</param>
        /// <returns></returns>
        public static void GetAllPassiveRadarBlocks(IMyCubeGrid grid, List<IMyFunctionalBlock> returnList)
        {
            _getAllPassiveRadarBlocks.Invoke(grid, returnList);
        }
        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all entries from all passive radars on <paramref name="grid"/> and from the grid's subgrids and datalink, and populates results in <paramref name="returnList"/>.<br/>
        /// Laggy!<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="grid">Grid to get entries from. Includes subgrids</param>
        /// <returns></returns>
        public static void GetAllPassiveSensorEntries(IMyCubeGrid grid, List<PassiveSensorEntry> returnList)
        {
            _getAllPassiveRadarEntries.Invoke(grid, _TuplePassiveRadarEntryDumpList);
            foreach (var entry in _TuplePassiveRadarEntryDumpList)
            {
                returnList.Add(new PassiveSensorEntry(entry));
            }
            _TuplePassiveRadarEntryDumpList.Clear();
        }

        /// <summary>
        /// Only callable on server.<br/>
        /// Gets all entries from all active and passive radars on <paramref name="grid"/> and from the grid's subgrids and datalink, and populates active results in <paramref name="activeReturnList"/>, and passive results in <paramref name="passiveReturnList"/>.<br/>
        /// Grids detected in activeReturnList will also not be returned in passiveReturnList.<br/>
        /// Laggy! (though less laggy than calling GetAllRadarEntries and GetAllPassiveRadarEntries separately)<br/>
        /// Not thread safe!
        /// </summary>
        /// <param name="grid">Grid to get entries from. Includes subgrids</param>
        /// <returns></returns>
        public static void GetAllDetectionEntries(IMyCubeGrid grid, List<RadarEntry> activeReturnList, List<PassiveSensorEntry> passiveReturnList)
        {
            _getAllDetectionEntries.Invoke(grid, _TupleRadarEntryDumpList, _TuplePassiveRadarEntryDumpList);
            foreach (var entry in _TupleRadarEntryDumpList)
            {
                activeReturnList.Add(new RadarEntry(entry));
            }
            foreach (var entry in _TuplePassiveRadarEntryDumpList)
            {
                passiveReturnList.Add(new PassiveSensorEntry(entry));
            }
            _TupleRadarEntryDumpList.Clear();
            _TuplePassiveRadarEntryDumpList.Clear();
        }

        private static List<TupleRadarEntryV2> _TupleRadarEntryDumpList;
        private static List<TupleJammingEntryV2> _TupleJammingEntryV2DumpList;
        private static List<TuplePassiveRadarEntry> _TuplePassiveRadarEntryDumpList;
        private static Action OnAPILoad;
        private static Func<IMyCubeGrid, IMyCubeGrid, bool> _canSee;
        private static Func<IMyCubeGrid, bool> _isGridSeen;
        private static Func<IMyCubeGrid, bool> _isGridLocked;
        private static Action<IMyCubeGrid, List<TupleRadarEntryV2>> _getAllRadarEntries;
        private static Action<IMyFunctionalBlock, List<TupleRadarEntryV2>> _getAllRadarEntriesRadar;
        private static Action<IMyFunctionalBlock, List<TupleJammingEntryV2>> _getAllJammingEntriesRadar;
        private static Action<IMyCubeGrid, List<TupleJammingEntryV2>> _getAllJammingEntries;
        private static Action<IMyCubeGrid, List<IMyFunctionalBlock>> _getAllRadarBlocks;
        private static Action<IMyCubeGrid, List<IMyTerminalBlock>> _getAllUpgradeBlocks;
        private static Action<IMyCubeGrid, List<IMyFunctionalBlock>> _getAllPassiveRadarBlocks;
        private static Action<IMyCubeGrid, List<TuplePassiveRadarEntry>> _getAllPassiveRadarEntries;
        private static Action<IMyCubeGrid, List<TupleRadarEntryV2>, List<TuplePassiveRadarEntry>> _getAllDetectionEntries;
        private static void OnModMessageRecieved(object obj)
        {
            if (IsReady)
            {
                return;
            }

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
                return;
            try
            {
                ApiAssign(dict);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("NebRadarAPI connection failed!");
                MyLog.Default.WriteLine(e);
                return;
            }
            _TupleRadarEntryDumpList = new List<TupleRadarEntryV2>();
            _TupleJammingEntryV2DumpList = new List<TupleJammingEntryV2>();
            _TuplePassiveRadarEntryDumpList = new List<TuplePassiveRadarEntry>();
            IsReady = true;

            OnAPILoad?.Invoke();
        }
        // core systems assign method
        private static void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            // base methods
            AssignMethod(delegates, "CanSee", ref _canSee);
            AssignMethod(delegates, "IsGridSeen", ref _isGridSeen);
            AssignMethod(delegates, "IsGridLocked", ref _isGridLocked);
            AssignMethod(delegates, "GetAllRadarEntriesV2", ref _getAllRadarEntries);
            AssignMethod(delegates, "GetAllRadarEntriesRadarV2", ref _getAllRadarEntriesRadar);
            AssignMethod(delegates, "GetAllJammingEntriesV2", ref _getAllJammingEntries);
            AssignMethod(delegates, "GetAllJammingEntriesRadarV2", ref _getAllJammingEntriesRadar);
            AssignMethod(delegates, "GetAllRadarBlocksV2", ref _getAllRadarBlocks);
            AssignMethod(delegates, "GetAllUpgradeBlocksV2", ref _getAllUpgradeBlocks);
            AssignMethod(delegates, "GetAllPassiveRadarBlocks", ref _getAllPassiveRadarBlocks);
            AssignMethod(delegates, "GetAllPassiveRadarEntries", ref _getAllPassiveRadarEntries);
            AssignMethod(delegates, "GetAllDetectionEntries", ref _getAllDetectionEntries);
        }
        // core systems assign method
        protected static void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"NebRadarAPI ERROR: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception($"NebRadarAPI ERROR: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }
        private static void SubscribeToEvent<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, T field) where T : class
        {
            if (delegates == null)
            {
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"NebRadarAPI ERROR: Couldn't find {name} delegate of type {typeof(T)}");

            if (del as Action<T> == null)
                throw new Exception($"NebRadarAPI ERROR: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
            (del as Action<T>).Invoke(field);
        }

        
    }
}
