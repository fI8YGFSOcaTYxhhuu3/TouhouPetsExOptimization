using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;

public static class System_Cache {

    public delegate void Delegate_BaseEnhance_TileDrawEffects( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData );
    public delegate void Delegate_BaseEnhance_NPCAI( NPC npc );
    public delegate bool? Delegate_BaseEnhance_NPCPreAI( NPC npc );
    public delegate void Delegate_BaseEnhance_ItemUpdateInventory( Item item, Player player );
    public delegate void Delegate_BaseEnhance_ItemPostDrawInInventory( Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale );

    public static Delegate_BaseEnhance_TileDrawEffects[] Actions_BaseEnhance_TileDrawEffects;
    public static Delegate_BaseEnhance_NPCAI[] Actions_BaseEnhance_NPCAI;
    public static Delegate_BaseEnhance_NPCPreAI[] Actions_BaseEnhance_NPCPreAI;
    public static Delegate_BaseEnhance_ItemPostDrawInInventory[] Actions_BaseEnhance_ItemPostDrawInInventory;
    public static Delegate_BaseEnhance_ItemUpdateInventory[] Actions_BaseEnhance_ItemUpdateInventory;

    public static int[] ItemToEnhanceIndex;
    public static Dictionary<string, int> EnhanceStringIdToIndex;

    public static void Unload() {
        Actions_BaseEnhance_TileDrawEffects = null;
        Actions_BaseEnhance_NPCAI = null;
        Actions_BaseEnhance_NPCPreAI = null;
        Actions_BaseEnhance_ItemPostDrawInInventory = null;
        Actions_BaseEnhance_ItemUpdateInventory = null;

        ItemToEnhanceIndex = null;
        EnhanceStringIdToIndex = null;

        System_PatchState.IsCacheBuilt = false;
    }

    public static void BuildCache() {
        System_PatchState.IsCacheBuilt = false;
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        try {
            ItemToEnhanceIndex = new int[ ItemLoader.ItemCount ];
            Array.Fill( ItemToEnhanceIndex, -1 );
            EnhanceStringIdToIndex = new Dictionary<string, int>();


            if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) {
                logger.Warn( "[System_Cache] 未找到前置模组 TouhouPetsEx，缓存构建中止。" );
                return;
            }

            Type baseEnhanceType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );
            Type registryType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhanceRegistry" );

            if ( baseEnhanceType == null ) {
                logger.Error( "[System_Cache] 致命错误：未找到类 TouhouPetsEx.Enhance.Core.BaseEnhance。" );
                return;
            }
            if ( registryType == null ) {
                logger.Error( "[System_Cache] 致命错误：未找到类 TouhouPetsEx.Enhance.Core.EnhanceRegistry。" );
                return;
            }

            PropertyInfo allEnhancementsProp = registryType.GetProperty( "AllEnhancements", BindingFlags.Static | BindingFlags.Public );
            MethodInfo getBoundItemsMethod = registryType.GetMethod( "GetBoundItemTypes", BindingFlags.Static | BindingFlags.Public );
            PropertyInfo enhanceIdProp = baseEnhanceType.GetProperty( "EnhanceId", BindingFlags.Instance | BindingFlags.Public );

            if ( allEnhancementsProp == null ) logger.Error( "[System_Cache] 致命错误：未找到属性 EnhanceRegistry.AllEnhancements。" );
            if ( getBoundItemsMethod == null ) logger.Error( "[System_Cache] 致命错误：未找到方法 EnhanceRegistry.GetBoundItemTypes。" );
            if ( enhanceIdProp == null ) logger.Error( "[System_Cache] 致命错误：未找到属性 BaseEnhance.EnhanceId。" );

            if ( allEnhancementsProp == null || getBoundItemsMethod == null || enhanceIdProp == null ) return;

            IEnumerable enhancementsEnum = allEnhancementsProp.GetValue( null ) as IEnumerable;
            if ( enhancementsEnum == null ) {
                logger.Warn( "[System_Cache] 警告：EnhanceRegistry.AllEnhancements 返回 null，无法构建缓存。" );
                return;
            }

            List<object> enhanceList = new List<object>();
            foreach ( object o in enhancementsEnum ) {
                if ( o != null ) enhanceList.Add( o );
            }
            int enhanceCount = enhanceList.Count;
            if ( enhanceCount == 0 ) {
                logger.Warn( "[System_Cache] 警告：EnhanceRegistry 返回了空列表 (Count=0)。原模组可能未加载任何增强。" );
                return;
            }

            Actions_BaseEnhance_TileDrawEffects = new Delegate_BaseEnhance_TileDrawEffects[ enhanceCount ];
            Actions_BaseEnhance_NPCAI = new Delegate_BaseEnhance_NPCAI[ enhanceCount ];
            Actions_BaseEnhance_NPCPreAI = new Delegate_BaseEnhance_NPCPreAI[ enhanceCount ];
            Actions_BaseEnhance_ItemPostDrawInInventory = new Delegate_BaseEnhance_ItemPostDrawInInventory[ enhanceCount ];
            Actions_BaseEnhance_ItemUpdateInventory = new Delegate_BaseEnhance_ItemUpdateInventory[ enhanceCount ];

            int validBindCount = 0;

            for ( int i = 0; i < enhanceCount; i++ ) {
                object enhanceInstance = enhanceList[ i ];


                object idObj = enhanceIdProp.GetValue( enhanceInstance );
                if ( idObj == null ) continue;
                string idStr = idObj.ToString();
                if ( !EnhanceStringIdToIndex.ContainsKey( idStr ) ) {
                    EnhanceStringIdToIndex[ idStr ] = i;
                }

                IEnumerable<int> boundItems = getBoundItemsMethod.Invoke( null, new object[] { idObj } ) as IEnumerable<int>;
                if ( boundItems != null ) {
                    foreach ( int itemId in boundItems ) {
                        if ( itemId >= 0 && itemId < ItemLoader.ItemCount ) {
                            ItemToEnhanceIndex[ itemId ] = i;
                            validBindCount++;
                        }
                    }
                }

                Type instanceType = enhanceInstance.GetType();
                Register( i, enhanceInstance, instanceType, baseEnhanceType, "TileDrawEffects", typeof( Delegate_BaseEnhance_TileDrawEffects ), Actions_BaseEnhance_TileDrawEffects, logger );
                Register( i, enhanceInstance, instanceType, baseEnhanceType, "NPCAI", typeof( Delegate_BaseEnhance_NPCAI ), Actions_BaseEnhance_NPCAI, logger );
                Register( i, enhanceInstance, instanceType, baseEnhanceType, "NPCPreAI", typeof( Delegate_BaseEnhance_NPCPreAI ), Actions_BaseEnhance_NPCPreAI, logger );
                Register( i, enhanceInstance, instanceType, baseEnhanceType, "ItemPostDrawInInventory", typeof( Delegate_BaseEnhance_ItemPostDrawInInventory ), Actions_BaseEnhance_ItemPostDrawInInventory, logger );
                Register( i, enhanceInstance, instanceType, baseEnhanceType, "ItemUpdateInventory", typeof( Delegate_BaseEnhance_ItemUpdateInventory ), Actions_BaseEnhance_ItemUpdateInventory, logger );
            }

            if ( validBindCount == 0 ) {
                logger.Warn( $"[System_Cache] 警告：已索引 {enhanceCount} 个增强实例，但未发现任何有效的物品绑定 (BindCount=0)。优化功能可能无实际作用。" );
                return;
            }
            System_PatchState.IsCacheBuilt = true;
            logger.Info( $"[System_Cache] 智能缓存构建成功。增强总数: {enhanceCount}, 物品绑定数: {validBindCount}" );
        }
        catch ( Exception e ) {
            System_PatchState.IsCacheBuilt = false;
            logger.Error( $"[System_Cache] 缓存构建过程中发生致命错误，优化将回退至原版。错误信息：\n{e}" );
        }
    }

    private static void Register( int index, object instance, Type instanceType, Type baseType, string methodName, Type delegateType, Array dispatchArray, log4net.ILog logger ) {
        MethodInfo method = instanceType.GetMethod( methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if ( method == null || method.DeclaringType == baseType ) return;

        try {
            dispatchArray.SetValue( method.CreateDelegate( delegateType, instance ), index );
        }
        catch ( Exception ex ) {
            logger.Debug( $"[System_Cache] 警告：无法为 {instanceType.Name} 绑定方法 {methodName} (Index: {index})。\n原因: {ex.Message}" );
        }
    }

}