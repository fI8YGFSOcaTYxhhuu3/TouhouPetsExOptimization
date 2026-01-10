using log4net;
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



public class System_缓存_静态数据 : ModSystem {

    private static readonly ILog 日志 = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;
    private static void 记录( string 文本 ) => 日志.Warn( $"[System_缓存_静态数据] {文本}" );

    public delegate void 委托_BaseEnhance_TileDrawEffects( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData );
    public delegate void 委托_BaseEnhance_NPCAI( NPC npc );
    public delegate bool? 委托_BaseEnhance_NPCPreAI( NPC npc );
    public delegate void 委托_BaseEnhance_ItemUpdateInventory( Item item, Player player );
    public delegate void 委托_BaseEnhance_ItemPostDrawInInventory( Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale );

    public static 委托_BaseEnhance_TileDrawEffects[] 委托映射_BaseEnhance_TileDrawEffects;
    public static 委托_BaseEnhance_NPCAI[] 委托映射_BaseEnhance_NPCAI;
    public static 委托_BaseEnhance_NPCPreAI[] 委托映射_BaseEnhance_NPCPreAI;
    public static 委托_BaseEnhance_ItemPostDrawInInventory[] 委托映射_BaseEnhance_ItemPostDrawInInventory;
    public static 委托_BaseEnhance_ItemUpdateInventory[] 委托映射_BaseEnhance_ItemUpdateInventory;

    public static int[] 宠物索引映射_物品ID;
    public static Dictionary<string, int> 宠物索引映射_宠物文本;

    public override void Unload() {
        委托映射_BaseEnhance_TileDrawEffects = null;
        委托映射_BaseEnhance_NPCAI = null;
        委托映射_BaseEnhance_NPCPreAI = null;
        委托映射_BaseEnhance_ItemPostDrawInInventory = null;
        委托映射_BaseEnhance_ItemUpdateInventory = null;
        宠物索引映射_物品ID = null;
        宠物索引映射_宠物文本 = null;
    }

    public static void 构筑() {
        System_补丁自检.缓存状态_静态数据 = false;

        try {
            宠物索引映射_物品ID = new int[ ItemLoader.ItemCount ]; Array.Fill( 宠物索引映射_物品ID, -1 );
            宠物索引映射_宠物文本 = new Dictionary<string, int>();

            Mod 目标模组 = TouhouPetsExOptimization.模组_东方小祖宗; if ( 目标模组 == null ) { 记录( "未找到前置模组 TouhouPetsEx" ); return; }

            Type 类型_BaseEnhance = 目标模组.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" ); if ( 类型_BaseEnhance == null ) { 记录( "未找到类 TouhouPetsEx.Enhance.Core.BaseEnhance" ); return; }
            Type 类型_EnhanceRegistry = 目标模组.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhanceRegistry" ); if ( 类型_EnhanceRegistry == null ) { 记录( "未找到类 TouhouPetsEx.Enhance.Core.EnhanceRegistry" ); return; }
            PropertyInfo 属性_AllEnhancements = 类型_EnhanceRegistry.GetProperty( "AllEnhancements", BindingFlags.Static | BindingFlags.Public ); if ( 属性_AllEnhancements == null ) { 记录( "未找到属性 EnhanceRegistry.AllEnhancements" ); return; }
            PropertyInfo 属性_EnhanceId = 类型_BaseEnhance.GetProperty( "EnhanceId", BindingFlags.Instance | BindingFlags.Public ); if ( 属性_EnhanceId == null ) { 记录( "未找到属性 BaseEnhance.EnhanceId" ); return; }
            MethodInfo 方法_GetBoundItemTypes = 类型_EnhanceRegistry.GetMethod( "GetBoundItemTypes", BindingFlags.Static | BindingFlags.Public ); if ( 方法_GetBoundItemTypes == null ) { 记录( "未找到方法 EnhanceRegistry.GetBoundItemTypes" ); return; }
            IEnumerable 对象_AllEnhancements = 属性_AllEnhancements.GetValue( null ) as IEnumerable; if ( 对象_AllEnhancements == null ) { 记录( "EnhanceRegistry.AllEnhancements 返回 null" ); return; }

            List<object> 列表_宠物能力 = new List<object>(); foreach ( object o in 对象_AllEnhancements ) { if ( o != null ) 列表_宠物能力.Add( o ); }
            int 计数_宠物能力 = 列表_宠物能力.Count;
            if ( 计数_宠物能力 == 0 ) { 记录( "EnhanceRegistry 为空列表" ); return; }

            委托映射_BaseEnhance_TileDrawEffects = new 委托_BaseEnhance_TileDrawEffects[ 计数_宠物能力 ];
            委托映射_BaseEnhance_NPCAI = new 委托_BaseEnhance_NPCAI[ 计数_宠物能力 ];
            委托映射_BaseEnhance_NPCPreAI = new 委托_BaseEnhance_NPCPreAI[ 计数_宠物能力 ];
            委托映射_BaseEnhance_ItemPostDrawInInventory = new 委托_BaseEnhance_ItemPostDrawInInventory[ 计数_宠物能力 ];
            委托映射_BaseEnhance_ItemUpdateInventory = new 委托_BaseEnhance_ItemUpdateInventory[ 计数_宠物能力 ];

            int 计数_注册物品 = 0;
            for ( int 宠物索引 = 0; 宠物索引 < 计数_宠物能力; 宠物索引++ ) {
                object 宠物能力 = 列表_宠物能力[ 宠物索引 ];
                object 宠物ID = 属性_EnhanceId.GetValue( 宠物能力 ); if ( 宠物ID == null ) continue;
                Type 宠物类型 = 宠物能力.GetType();

                宠物索引映射_宠物文本.TryAdd( 宠物ID.ToString(), 宠物索引 );
                if ( 方法_GetBoundItemTypes.Invoke( null, [ 宠物ID ] ) is IEnumerable<int> 相关物品ID ) foreach ( int 物品ID in 相关物品ID ) {
                        if ( 宠物索引映射_物品ID[ 物品ID ] != -1 ) {
                            string 冲突物品名称 = Lang.GetItemNameValue( 物品ID );
                            int 已有宠物索引 = 宠物索引映射_物品ID[ 物品ID ];
                            object 已有宠物能力 = 列表_宠物能力[ 已有宠物索引 ];
                            object 已有宠物ID = 属性_EnhanceId.GetValue( 已有宠物能力 );
                            记录( $"物品 [{冲突物品名称}] (ID:{物品ID}) 存在多重绑定冲突。\n" +
                                  $"   -> 原绑定：{已有宠物ID} (Index:{已有宠物索引})\n" +
                                  $"   -> 新绑定：{宠物ID} (Index:{宠物索引})" );
                        }
                        宠物索引映射_物品ID[ 物品ID ] = 宠物索引; 
                        计数_注册物品++; 
                    }

                注册宠物函数( 宠物索引, 宠物能力, 宠物类型, 类型_BaseEnhance, "TileDrawEffects", typeof( 委托_BaseEnhance_TileDrawEffects ), 委托映射_BaseEnhance_TileDrawEffects );
                注册宠物函数( 宠物索引, 宠物能力, 宠物类型, 类型_BaseEnhance, "NPCAI", typeof( 委托_BaseEnhance_NPCAI ), 委托映射_BaseEnhance_NPCAI );
                注册宠物函数( 宠物索引, 宠物能力, 宠物类型, 类型_BaseEnhance, "NPCPreAI", typeof( 委托_BaseEnhance_NPCPreAI ), 委托映射_BaseEnhance_NPCPreAI );
                注册宠物函数( 宠物索引, 宠物能力, 宠物类型, 类型_BaseEnhance, "ItemPostDrawInInventory", typeof( 委托_BaseEnhance_ItemPostDrawInInventory ), 委托映射_BaseEnhance_ItemPostDrawInInventory );
                注册宠物函数( 宠物索引, 宠物能力, 宠物类型, 类型_BaseEnhance, "ItemUpdateInventory", typeof( 委托_BaseEnhance_ItemUpdateInventory ), 委托映射_BaseEnhance_ItemUpdateInventory );
            }
            if ( 计数_注册物品 == 0 ) { 记录( $"{计数_宠物能力} 个增强实例没有任何相关物品" ); return; }
        }
        catch ( Exception 异常 ) { 记录( $"发生未知错误：\n{异常}" ); return; }

        System_补丁自检.缓存状态_静态数据 = true;
    }

    private static void 注册宠物函数( int 宠物索引, object 宠物能力, Type 宠物类型, Type 宠物基类, string 函数名称, Type 委托, Array 委托映射 ) {
        MethodInfo 函数 = 宠物类型.GetMethod( 函数名称, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 函数 == null || 函数.DeclaringType == 宠物基类 ) return;
        try { 委托映射.SetValue( 函数.CreateDelegate( 委托, 宠物能力 ), 宠物索引 ); }
        catch ( Exception 异常 ) { 记录( $"无法为 {宠物类型.Name} 绑定方法 {函数名称} (Index: {宠物索引})。\n原因: {异常.Message}" ); }
    }

}