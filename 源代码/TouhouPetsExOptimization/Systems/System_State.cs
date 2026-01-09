using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_State : ModSystem {

    public static List<int> ActiveIndices_TileDrawEffects = new List<int>();
    public static List<int> ActiveIndices_NPCAI = new List<int>();
    public static List<int> ActiveIndices_NPCPreAI = new List<int>();
    public static List<int> ActiveIndices_ItemUpdateInventory = new List<int>();
    public static List<int> ActiveIndices_ItemPostDrawInInventory = new List<int>();
    private static bool[] _visitedIndices;

    private static ModPlayer _prototypeEnhancePlayer;
    private static FieldInfo _fieldActiveEnhance;
    private static FieldInfo _fieldActivePassiveEnhance;

    private const int UPDATE_INTERVAL = 60;
    private static int _updateTimer = 0;

    private static bool _hasLoggedRuntimeError = false;

    public override void Load() {
        ActiveIndices_TileDrawEffects = new List<int>( 32 );
        ActiveIndices_NPCAI = new List<int>( 32 );
        ActiveIndices_NPCPreAI = new List<int>( 32 );
        ActiveIndices_ItemUpdateInventory = new List<int>( 32 );
        ActiveIndices_ItemPostDrawInInventory = new List<int>( 32 );
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
        if ( System_PatchState.IsCacheBuilt && System_Cache.Actions_BaseEnhance_TileDrawEffects != null ) {
            _visitedIndices = new bool[ System_Cache.Actions_BaseEnhance_TileDrawEffects.Length ];
        }
        if ( !System_PatchState.IsSafeToOptimize ) {
            Mod.Logger.Warn( $"[System_State] 初始化未完全成功 (Reader: {System_PatchState.IsActivePetListReaderWorking}, Cache: {System_PatchState.IsCacheBuilt})。优化功能将自动禁用以回退至原版逻辑。" );
        }
    }

    public override void Unload() {
        ActiveIndices_TileDrawEffects = null;
        ActiveIndices_NPCAI = null;
        ActiveIndices_NPCPreAI = null;
        ActiveIndices_ItemUpdateInventory = null;
        ActiveIndices_ItemPostDrawInInventory = null;
        _visitedIndices = null;
        _prototypeEnhancePlayer = null;
        _fieldActiveEnhance = null;
        _fieldActivePassiveEnhance = null;

        System_PatchState.IsIdMappingWorking = false;
        System_PatchState.IsActivePetListReaderWorking = false;
    }

    public override void PostUpdateEverything() {
        if ( Main.gameMenu ) {
            ClearAllLists();
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

            RefreshLists( activeList, passiveList );
        }
        catch ( Exception ex ) {
            System_PatchState.IsActivePetListReaderWorking = false;
            ClearAllLists();
            if ( !_hasLoggedRuntimeError ) {
                Mod.Logger.Error( "[System_State] 运行时读取玩家数据失败！优化功能已紧急熔断回退至原版逻辑。", ex );
                _hasLoggedRuntimeError = true;
            }
        }
    }

    private void ClearAllLists() {
        if ( ActiveIndices_TileDrawEffects?.Count > 0 ) ActiveIndices_TileDrawEffects.Clear();
        if ( ActiveIndices_NPCAI?.Count > 0 ) ActiveIndices_NPCAI.Clear();
        if ( ActiveIndices_NPCPreAI?.Count > 0 ) ActiveIndices_NPCPreAI.Clear();
        if ( ActiveIndices_ItemUpdateInventory?.Count > 0 ) ActiveIndices_ItemUpdateInventory.Clear();
        if ( ActiveIndices_ItemPostDrawInInventory?.Count > 0 ) ActiveIndices_ItemPostDrawInInventory.Clear();
    }

    private void RefreshLists( IList activeList, IList passiveList ) {
        ClearAllLists();

        if ( _visitedIndices == null ) return;
        Array.Clear( _visitedIndices, 0, _visitedIndices.Length );

        ResolveAndDistribute( activeList );
        ResolveAndDistribute( passiveList );
    }

    private void ResolveAndDistribute( IList list ) {
        if ( list == null ) return;
        foreach ( object item in list ) {
            string idString = item.ToString();
            if ( System_Cache.EnhanceStringIdToIndex.TryGetValue( idString, out int index ) ) {
                if ( index < 0 || index >= _visitedIndices.Length ) continue;

                if ( _visitedIndices[ index ] ) continue;
                _visitedIndices[ index ] = true;
                
                if ( System_Cache.Actions_BaseEnhance_TileDrawEffects[ index ] != null ) ActiveIndices_TileDrawEffects.Add( index );
                if ( System_Cache.Actions_BaseEnhance_NPCAI[ index ] != null ) ActiveIndices_NPCAI.Add( index );
                if ( System_Cache.Actions_BaseEnhance_NPCPreAI[ index ] != null ) ActiveIndices_NPCPreAI.Add( index );
                if ( System_Cache.Actions_BaseEnhance_ItemPostDrawInInventory[ index ] != null ) ActiveIndices_ItemPostDrawInInventory.Add( index );
                if ( System_Cache.Actions_BaseEnhance_ItemUpdateInventory[ index ] != null ) ActiveIndices_ItemUpdateInventory.Add( index );
            }
        }
    }

}