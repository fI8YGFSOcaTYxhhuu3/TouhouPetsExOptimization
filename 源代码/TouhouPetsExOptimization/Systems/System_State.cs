using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_State : ModSystem {

    public static List<int> ActiveEnhanceIndices = new List<int>();

    private static ModPlayer _prototypeEnhancePlayer;
    private static FieldInfo _fieldActiveEnhance;
    private static FieldInfo _fieldActivePassiveEnhance;

    private const int UPDATE_INTERVAL = 60;
    private static int _updateTimer = 0;

    private static bool _hasLoggedRuntimeError = false;

    public override void Load() {
        ActiveEnhanceIndices = new List<int>( 64 );
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
            Type enhancePlayersType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhancePlayers" );

            if ( enhancePlayersType == null ) {
                Mod.Logger.Warn( "[System_State] 警告：未找到类 TouhouPetsEx.Enhance.Core.EnhancePlayers" );
            }
            else {
                _fieldActiveEnhance = enhancePlayersType.GetField( "ActiveEnhance", BindingFlags.Instance | BindingFlags.Public );
                _fieldActivePassiveEnhance = enhancePlayersType.GetField( "ActivePassiveEnhance", BindingFlags.Instance | BindingFlags.Public );

                if ( _fieldActiveEnhance == null ) Mod.Logger.Warn( "[System_State] 警告：未找到字段 EnhancePlayers.ActiveEnhance" );
                if ( _fieldActivePassiveEnhance == null ) Mod.Logger.Warn( "[System_State] 警告：未找到字段 EnhancePlayers.ActivePassiveEnhance" );

                if ( targetMod.TryFind( "EnhancePlayers", out ModPlayer mp ) ) {
                    _prototypeEnhancePlayer = mp;
                    if ( _fieldActiveEnhance != null && _fieldActivePassiveEnhance != null ) {
                        System_PatchState.IsActivePetListReaderWorking = true;
                    }
                }
                else {
                    Mod.Logger.Warn( "[System_State] 警告：未找到 ModPlayer 实例 TouhouPetsEx.EnhancePlayers" );
                }
            }
        }
        catch ( Exception ex ) {
            Mod.Logger.Error( "[System_State] 初始化玩家读取器时发生未处理异常：", ex );
            System_PatchState.IsActivePetListReaderWorking = false;
        }

        System_Cache.BuildCache();
        System_PatchState.IsIdMappingWorking = System_PatchState.IsCacheBuilt;
        if ( !System_PatchState.IsSafeToOptimize ) {
            Mod.Logger.Warn( $"[System_State] 初始化未完全成功 (Reader: {System_PatchState.IsActivePetListReaderWorking}, Cache: {System_PatchState.IsCacheBuilt})。优化功能将自动禁用以回退至原版逻辑。" );
        }
    }

    public override void Unload() {
        ActiveEnhanceIndices = null;
        _prototypeEnhancePlayer = null;
        _fieldActiveEnhance = null;
        _fieldActivePassiveEnhance = null;

        System_PatchState.IsIdMappingWorking = false;
        System_PatchState.IsActivePetListReaderWorking = false;
    }

    public override void PostUpdateEverything() {
        if ( Main.gameMenu ) {
            if ( ActiveEnhanceIndices != null && ActiveEnhanceIndices.Count > 0 )
                ActiveEnhanceIndices = new List<int>();

            _updateTimer = UPDATE_INTERVAL;
            return;
        }

        _updateTimer++;
        if ( _updateTimer < UPDATE_INTERVAL ) return;
        _updateTimer = 0;

        if ( !System_PatchState.IsSafeToOptimize ) return;
        if ( Main.LocalPlayer == null || !Main.LocalPlayer.active ) return;

        try {
            ModPlayer enhancePlayerInstance = Main.LocalPlayer.GetModPlayer( _prototypeEnhancePlayer );
            if ( enhancePlayerInstance == null ) return;

            var activeList = _fieldActiveEnhance.GetValue( enhancePlayerInstance ) as IList;
            var passiveList = _fieldActivePassiveEnhance.GetValue( enhancePlayerInstance ) as IList;

            List<int> newIndices = new List<int>( 64 );
            ResolveAndAdd( activeList, newIndices );
            ResolveAndAdd( passiveList, newIndices );
            ActiveEnhanceIndices = newIndices;
        }
        catch ( Exception ex ) {
            System_PatchState.IsActivePetListReaderWorking = false;
            ActiveEnhanceIndices = new List<int>();
            if ( !_hasLoggedRuntimeError ) {
                Mod.Logger.Error( "[System_State] 运行时读取玩家数据失败！优化功能已紧急熔断回退至原版逻辑。", ex );
                _hasLoggedRuntimeError = true;
            }
        }
    }

    private void ResolveAndAdd( IList list, List<int> targetList ) {
        if ( list == null ) return;
        foreach ( object item in list ) {
            string idString = item.ToString();
            if ( System_Cache.EnhanceStringIdToIndex.TryGetValue( idString, out int index ) ) {
                if ( !targetList.Contains( index ) ) {
                    targetList.Add( index );
                }
            }
        }
    }

}