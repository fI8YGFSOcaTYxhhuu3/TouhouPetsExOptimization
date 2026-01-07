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

    public static Delegate_BaseEnhance_TileDrawEffects[] Dispatch_BaseEnhance_TileDrawEffects;
    public static Delegate_BaseEnhance_NPCAI[] Dispatch_BaseEnhance_NPCAI;
    public static Delegate_BaseEnhance_NPCPreAI[] Dispatch_BaseEnhance_NPCPreAI;
    public static Delegate_BaseEnhance_ItemPostDrawInInventory[] Dispatch_BaseEnhance_ItemPostDrawInInventory;
    public static Delegate_BaseEnhance_ItemUpdateInventory[] Dispatch_BaseEnhance_ItemUpdateInventory;

    public static void Unload() {
        Dispatch_BaseEnhance_TileDrawEffects = null;
        Dispatch_BaseEnhance_NPCAI = null;
        Dispatch_BaseEnhance_NPCPreAI = null;
        Dispatch_BaseEnhance_ItemPostDrawInInventory = null;
        Dispatch_BaseEnhance_ItemUpdateInventory = null;

        System_PatchState.IsCacheBuilt = false;
    }

    public static void BuildCache() {
        System_PatchState.IsCacheBuilt = false;
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        try {
            Dispatch_BaseEnhance_TileDrawEffects = new Delegate_BaseEnhance_TileDrawEffects[ ItemLoader.ItemCount ];
            Dispatch_BaseEnhance_NPCAI = new Delegate_BaseEnhance_NPCAI[ ItemLoader.ItemCount ];
            Dispatch_BaseEnhance_NPCPreAI = new Delegate_BaseEnhance_NPCPreAI[ ItemLoader.ItemCount ];
            Dispatch_BaseEnhance_ItemPostDrawInInventory = new Delegate_BaseEnhance_ItemPostDrawInInventory[ ItemLoader.ItemCount ];
            Dispatch_BaseEnhance_ItemUpdateInventory = new Delegate_BaseEnhance_ItemUpdateInventory[ ItemLoader.ItemCount ];

            if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) {
                logger.Error( "[System_Cache] 致命错误：未找到模组 TouhouPetsEx，缓存构建中止。" );
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

            IEnumerable enhancements = allEnhancementsProp.GetValue( null ) as IEnumerable;
            if ( enhancements == null ) {
                logger.Warn( "[System_Cache] 警告：EnhanceRegistry.AllEnhancements 返回 null，无法构建缓存。" );
                return;
            }

            int count = 0;
            foreach ( object enhanceInstance in enhancements ) {
                if ( enhanceInstance == null ) continue;

                object id = enhanceIdProp.GetValue( enhanceInstance );
                if ( id == null ) continue;

                IEnumerable<int> boundItems = getBoundItemsMethod.Invoke( null, new object[] { id } ) as IEnumerable<int>;
                if ( boundItems == null ) continue;

                Type instanceType = enhanceInstance.GetType();

                foreach ( int itemId in boundItems ) {
                    if ( itemId < 0 || itemId >= ItemLoader.ItemCount ) continue;

                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "TileDrawEffects", typeof( Delegate_BaseEnhance_TileDrawEffects ), Dispatch_BaseEnhance_TileDrawEffects, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "NPCAI", typeof( Delegate_BaseEnhance_NPCAI ), Dispatch_BaseEnhance_NPCAI, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "NPCPreAI", typeof( Delegate_BaseEnhance_NPCPreAI ), Dispatch_BaseEnhance_NPCPreAI, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "ItemPostDrawInInventory", typeof( Delegate_BaseEnhance_ItemPostDrawInInventory ), Dispatch_BaseEnhance_ItemPostDrawInInventory, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "ItemUpdateInventory", typeof( Delegate_BaseEnhance_ItemUpdateInventory ), Dispatch_BaseEnhance_ItemUpdateInventory, logger );
                    
                    count++;
                }
            }

            if ( count == 0 ) {
                logger.Warn( "[System_Cache] 警告：虽然获取到了增强列表，但未建立任何物品绑定 (Count=0)。原模组可能尚未初始化完毕。" );
            }
            else {
                System_PatchState.IsCacheBuilt = true;
                logger.Info( $"[System_Cache] 智能缓存构建成功，共索引 {count} 个绑定条目。" );
            }
        }
        catch ( Exception e ) {
            System_PatchState.IsCacheBuilt = false;
            logger.Error( $"[System_Cache] 缓存构建过程中发生致命错误，优化将回退至原版。错误信息：\n{e}" );
        }
    }

    private static void Register( int itemId, object instance, Type instanceType, Type baseType, string methodName, Type delegateType, Array dispatchArray, log4net.ILog logger ) {
        MethodInfo method = instanceType.GetMethod( methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if ( method == null || method.DeclaringType == baseType ) return;

        try {
            dispatchArray.SetValue( method.CreateDelegate( delegateType, instance ), itemId );
        }
        catch ( Exception ex ) {
            logger.Debug( $"[System_Cache] 警告：无法为 {instanceType.Name} 绑定方法 {methodName} (ItemID: {itemId})。\n原因: {ex.Message}" );
        }
    }

}