using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
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
                logger.Warn( "[System_Cache] 优化中止：未找到模组 TouhouPetsEx。" );
                return;
            }

            Type baseEnhanceType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );
            Type mainType = targetMod.Code.GetType( "TouhouPetsEx.TouhouPetsEx" );

            if ( baseEnhanceType == null ) {
                logger.Warn( "[System_Cache] 优化中止：未找到类 TouhouPetsEx.Enhance.Core.BaseEnhance。" );
                return;
            }
            if ( mainType == null ) {
                logger.Warn( "[System_Cache] 优化中止：未找到主类 TouhouPetsEx.TouhouPetsEx。" );
                return;
            }

            FieldInfo instancesField = mainType.GetField( "GEnhanceInstances", BindingFlags.Static | BindingFlags.Public );
            if ( instancesField == null ) {
                logger.Warn( "[System_Cache] 优化中止：未找到字段 GEnhanceInstances。" );
                return;
            }

            IDictionary instances = instancesField.GetValue( null ) as IDictionary;
            if ( instances == null ) {
                logger.Warn( "[System_Cache] 优化中止：GEnhanceInstances 字段值为 null。" );
                return;
            }

            foreach ( DictionaryEntry entry in instances ) {
                if ( entry.Key is int itemId && entry.Value != null ) {
                    object enhanceInstance = entry.Value;
                    Type instanceType = enhanceInstance.GetType();

                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "TileDrawEffects", typeof( Delegate_BaseEnhance_TileDrawEffects ), Dispatch_BaseEnhance_TileDrawEffects, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "NPCAI", typeof( Delegate_BaseEnhance_NPCAI ), Dispatch_BaseEnhance_NPCAI, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "NPCPreAI", typeof( Delegate_BaseEnhance_NPCPreAI ), Dispatch_BaseEnhance_NPCPreAI, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "ItemPostDrawInInventory", typeof( Delegate_BaseEnhance_ItemPostDrawInInventory ), Dispatch_BaseEnhance_ItemPostDrawInInventory, logger );
                    Register( itemId, enhanceInstance, instanceType, baseEnhanceType, "ItemUpdateInventory", typeof( Delegate_BaseEnhance_ItemUpdateInventory ), Dispatch_BaseEnhance_ItemUpdateInventory, logger );
                }
            }

            System_PatchState.IsCacheBuilt = true;
            logger.Info( "[System_Cache] 智能缓存构建成功。" );
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