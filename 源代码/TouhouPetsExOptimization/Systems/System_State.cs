using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_State : ModSystem {

    public static List<int> LocalPlayerActivePets = new List<int>();

    private static ModPlayer _prototypeEnhancePlayer;
    private static FieldInfo _fieldActiveEnhance;
    private static FieldInfo _fieldActivePassiveEnhance;

    private static Dictionary<string, int> _idToItemTypeMap;

    private const int UPDATE_INTERVAL = 60;
    private static int _updateTimer = 0;

    private static bool _hasLoggedRuntimeError = false;

    public override void Load() {
        LocalPlayerActivePets = new List<int>( 64 );
        _idToItemTypeMap = new Dictionary<string, int>();
        _hasLoggedRuntimeError = false;

        System_PatchState.IsIdMappingWorking = false;
        System_PatchState.IsActivePetListReaderWorking = false;
    }

    public override void PostSetupContent() {
        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) {
            Mod.Logger.Warn( "[System_State] 未找到前置模组 TouhouPetsEx，优化系统将暂停工作。" );
            return;
        }

        try {
            Type registryType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhanceRegistry" );
            if ( registryType != null ) {
                PropertyInfo allEnhancesProp = registryType.GetProperty( "AllEnhancements", BindingFlags.Static | BindingFlags.Public );
                MethodInfo getBoundItemsMethod = registryType.GetMethod( "GetBoundItemTypes", BindingFlags.Static | BindingFlags.Public );

                if ( allEnhancesProp != null && getBoundItemsMethod != null ) {
                    IEnumerable allEnhances = allEnhancesProp.GetValue( null ) as IEnumerable;
                    if ( allEnhances != null ) {
                        foreach ( object enhanceObj in allEnhances ) {
                            if ( enhanceObj == null ) continue;

                            PropertyInfo idProp = enhanceObj.GetType().GetProperty( "EnhanceId", BindingFlags.Instance | BindingFlags.Public );
                            if ( idProp == null ) continue;

                            object enhanceIdStruct = idProp.GetValue( enhanceObj );

                            object boundItemsObj = getBoundItemsMethod.Invoke( null, [enhanceIdStruct] );
                            IEnumerable<int> boundItems = boundItemsObj as IEnumerable<int>;

                            if ( boundItems != null ) {
                                string idString = enhanceIdStruct.ToString();

                                foreach ( int itemType in boundItems ) {
                                    if ( !_idToItemTypeMap.ContainsKey( idString ) ) {
                                        _idToItemTypeMap[ idString ] = itemType;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if ( _idToItemTypeMap.Count == 0 ) Mod.Logger.Warn( "[System_State] 警告：ID 映射表构建完成，但内容为空。无法从 EnhanceRegistry 读取数据。" );
            else {
                System_PatchState.IsIdMappingWorking = true;
                Mod.Logger.Info( $"[System_State] ID 映射表构建成功，共索引 {_idToItemTypeMap.Count} 个条目。" );
            }
        }
        catch ( Exception ex ) {
            Mod.Logger.Error( "[System_State] 构建 ID 映射表时发生未处理异常：", ex );
            System_PatchState.IsIdMappingWorking = false;
        }

        try {
            Type enhancePlayersType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhancePlayers" );
            if ( enhancePlayersType != null ) {
                _fieldActiveEnhance = enhancePlayersType.GetField( "ActiveEnhance", BindingFlags.Instance | BindingFlags.Public );
                _fieldActivePassiveEnhance = enhancePlayersType.GetField( "ActivePassiveEnhance", BindingFlags.Instance | BindingFlags.Public );

                if ( targetMod.TryFind( "EnhancePlayers", out ModPlayer mp ) ) {
                    _prototypeEnhancePlayer = mp;
                    if ( _fieldActiveEnhance != null && _fieldActivePassiveEnhance != null ) {
                        System_PatchState.IsActivePetListReaderWorking = true;
                    }
                }
            }
        }
        catch ( Exception ex ) {
            Mod.Logger.Error( "[System_State] 初始化玩家读取器时发生未处理异常：", ex );
            System_PatchState.IsActivePetListReaderWorking = false;
        }

        if ( !System_PatchState.IsSafeToOptimize ) {
            Mod.Logger.Warn( $"[System_State] 初始化未完全成功 (IDMap: {System_PatchState.IsIdMappingWorking}, Reader: {System_PatchState.IsActivePetListReaderWorking})。优化功能将自动禁用。" );
        }

        System_Cache.BuildCache();
    }

    public override void Unload() {
        LocalPlayerActivePets = null;
        _prototypeEnhancePlayer = null;
        _fieldActiveEnhance = null;
        _fieldActivePassiveEnhance = null;
        _idToItemTypeMap = null;
        System_PatchState.IsIdMappingWorking = false;
        System_PatchState.IsActivePetListReaderWorking = false;
    }

    public override void PostUpdateEverything() {
        if ( Main.gameMenu ) {
            LocalPlayerActivePets.Clear();
            _updateTimer = UPDATE_INTERVAL;
            return;
        }

        _updateTimer++;
        if ( _updateTimer < UPDATE_INTERVAL ) return;
        _updateTimer = 0;

        LocalPlayerActivePets.Clear();

        if ( !System_PatchState.IsSafeToOptimize ) return;
        if ( Main.LocalPlayer == null || !Main.LocalPlayer.active ) return;

        try {
            ModPlayer enhancePlayerInstance = Main.LocalPlayer.GetModPlayer( _prototypeEnhancePlayer );
            if ( enhancePlayerInstance == null ) return;

            var activeList = _fieldActiveEnhance.GetValue( enhancePlayerInstance ) as IList;
            var passiveList = _fieldActivePassiveEnhance.GetValue( enhancePlayerInstance ) as IList;

            ResolveAndAdd( activeList );
            ResolveAndAdd( passiveList );
        }
        catch ( Exception ex ) {
            System_PatchState.IsActivePetListReaderWorking = false;
            if ( !_hasLoggedRuntimeError ) {
                Mod.Logger.Error( "[System_State] 运行时读取玩家数据失败！优化功能已紧急熔断回退。", ex );
                _hasLoggedRuntimeError = true;
            }
        }
    }

    private void ResolveAndAdd( IList list ) {
        if ( list == null ) return;
        foreach ( object item in list ) {
            string idString = item.ToString();
            if ( _idToItemTypeMap.TryGetValue( idString, out int itemType ) ) {
                LocalPlayerActivePets.Add( itemType );
            }
        }
    }

}