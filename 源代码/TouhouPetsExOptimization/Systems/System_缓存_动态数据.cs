using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_缓存_动态数据 : ModSystem {

    private static readonly ILog 日志 = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;
    private static void 记录( string 文本 ) => 日志.Warn( $"[System_缓存_动态数据] {文本}" );

    public static List<int> 生效宠物索引_TileDrawEffects = [];
    public static List<int> 生效宠物索引_NPCAI = [];
    public static List<int> 生效宠物索引_NPCPreAI = [];
    public static List<int> 生效宠物索引_ItemUpdateInventory = [];
    public static List<int> 生效宠物索引_ItemPostDrawInInventory = [];
    private static bool[] 索引重复状态_全局;
    private static bool[] 索引重复状态_本地;

    private static int 模组玩家索引_EnhancePlayers = -1;
    private static FieldInfo 字段_ActiveEnhance;
    private static FieldInfo 字段_ActivePassiveEnhance;

    private const int 更新间隔 = 60;
    private static int 更新计时 = 0;

    private static bool 已打印错误 = false;

    public override void Load() {
        生效宠物索引_TileDrawEffects = new(32);
        生效宠物索引_NPCAI = new(32);
        生效宠物索引_NPCPreAI = new(32);
        生效宠物索引_ItemUpdateInventory = new(32);
        生效宠物索引_ItemPostDrawInInventory = new(32);
        已打印错误 = false;
    }

    public override void Unload() {
        生效宠物索引_TileDrawEffects = null;
        生效宠物索引_NPCAI = null;
        生效宠物索引_NPCPreAI = null;
        生效宠物索引_ItemUpdateInventory = null;
        生效宠物索引_ItemPostDrawInInventory = null;
        索引重复状态_全局 = null;
        索引重复状态_本地 = null;
        字段_ActiveEnhance = null;
        字段_ActivePassiveEnhance = null;
    }

    public static void 构筑() {
        System_补丁自检.缓存状态_动态数据 = false;

        try {
            if ( !System_补丁自检.缓存状态_静态数据 ) return;
            索引重复状态_全局 = new bool[ System_缓存_静态数据.委托映射_BaseEnhance_TileDrawEffects.Length ];
            索引重复状态_本地 = new bool[ System_缓存_静态数据.委托映射_BaseEnhance_TileDrawEffects.Length ];

            Mod 目标模组 = TouhouPetsExOptimization.模组_东方小祖宗; if ( 目标模组 == null ) { 记录( "未找到前置模组 TouhouPetsEx" ); return; }

            if ( !目标模组.TryFind( "EnhancePlayers", out ModPlayer 模组玩家模板 ) ) { 记录( "未找到 ModPlayer 实例 TouhouPetsEx.EnhancePlayers" ); return; }
            模组玩家索引_EnhancePlayers = 模组玩家模板.Index;

            Type 类型_EnhancePlayers = 目标模组.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhancePlayers" ); if ( 类型_EnhancePlayers == null ) { 记录( "未找到类 TouhouPetsEx.Enhance.Core.EnhancePlayers" ); return; }
            字段_ActiveEnhance = 类型_EnhancePlayers.GetField( "ActiveEnhance", BindingFlags.Instance | BindingFlags.Public ); if ( 字段_ActiveEnhance == null ) { 记录( "未找到字段 EnhancePlayers.ActiveEnhance。" ); return; }
            字段_ActivePassiveEnhance = 类型_EnhancePlayers.GetField( "ActivePassiveEnhance", BindingFlags.Instance | BindingFlags.Public ); if ( 字段_ActivePassiveEnhance == null ) { 记录( "未找到字段 EnhancePlayers.ActivePassiveEnhance" ); return; }

            System_补丁自检.缓存状态_动态数据 = true;
        }
        catch ( Exception 异常 ) { 记录( $"获取玩家字段时发生未知异常：\n{ 异常 }" ); }
    }

    public override void PostUpdateEverything() {
        if ( Main.gameMenu ) { 清空宠物缓存(); 更新计时 = 更新间隔; return; }
        if ( !System_补丁自检.未发生已知错误 ) return;

        更新计时++; if ( 更新计时 < 更新间隔 ) return; 更新计时 = 0;

        try {
            清空宠物缓存();

            for ( int i = 0; i < Main.maxPlayers; i++ ) {
                Player 玩家 = Main.player[ i ]; if ( !玩家.active ) continue;
                var 模组玩家 = 玩家.ModPlayers[ 模组玩家索引_EnhancePlayers ];
                var 主动宠物列表 = 字段_ActiveEnhance.GetValue( 模组玩家 ) as IList;
                var 被动宠物列表 = 字段_ActivePassiveEnhance.GetValue( 模组玩家 ) as IList;
                构筑全局缓存( 主动宠物列表 );
                构筑全局缓存( 被动宠物列表 );
            }

            if ( Main.LocalPlayer.active ) {
                var 模组玩家 = Main.LocalPlayer.ModPlayers[ 模组玩家索引_EnhancePlayers ];
                var 主动宠物列表 = 字段_ActiveEnhance.GetValue( 模组玩家 ) as IList;
                var 被动宠物列表 = 字段_ActivePassiveEnhance.GetValue( 模组玩家 ) as IList;
                构筑本地缓存( 主动宠物列表 );
                构筑本地缓存( 被动宠物列表 );
            }
        }
        catch ( Exception 异常 ) {
            System_补丁自检.缓存状态_动态数据 = false;
            清空宠物缓存();
            if ( !已打印错误 ) { 记录( $"运行时读取玩家数据失败：\n{异常}" ); 已打印错误 = true; }
        }
    }

    private void 清空宠物缓存() {
        生效宠物索引_TileDrawEffects.Clear();
        生效宠物索引_NPCAI.Clear();
        生效宠物索引_NPCPreAI.Clear();
        生效宠物索引_ItemUpdateInventory.Clear();
        生效宠物索引_ItemPostDrawInInventory.Clear();
        Array.Clear( 索引重复状态_全局, 0, 索引重复状态_全局.Length );
        Array.Clear( 索引重复状态_本地, 0, 索引重复状态_本地.Length );
    }

    private void 构筑全局缓存( IList 生效宠物列表 ) {
        if ( 生效宠物列表 == null ) return;
        foreach ( object 宠物 in 生效宠物列表 ) {
            if ( 宠物 is not string 宠物文本 ) continue;
            if ( !System_缓存_静态数据.宠物索引映射_宠物文本.TryGetValue( 宠物文本, out int 宠物索引 ) ) continue;
            if ( 索引重复状态_全局[ 宠物索引 ] ) continue; else 索引重复状态_全局[ 宠物索引 ] = true;

            if ( System_缓存_静态数据.委托映射_BaseEnhance_TileDrawEffects[ 宠物索引 ] != null ) 生效宠物索引_TileDrawEffects.Add( 宠物索引 );
            if ( System_缓存_静态数据.委托映射_BaseEnhance_NPCAI[ 宠物索引 ] != null ) 生效宠物索引_NPCAI.Add( 宠物索引 );
            if ( System_缓存_静态数据.委托映射_BaseEnhance_NPCPreAI[ 宠物索引 ] != null ) 生效宠物索引_NPCPreAI.Add( 宠物索引 );
        }
    }

    private void 构筑本地缓存( IList 生效宠物列表 ) {
        if ( 生效宠物列表 == null ) return;
        foreach ( object 宠物 in 生效宠物列表 ) {
            if ( 宠物 is not string 宠物文本 ) continue;
            if ( !System_缓存_静态数据.宠物索引映射_宠物文本.TryGetValue( 宠物文本, out int 宠物索引 ) ) continue;
            if ( 索引重复状态_本地[ 宠物索引 ] ) continue; else 索引重复状态_本地[ 宠物索引 ] = true;

            if ( System_缓存_静态数据.委托映射_BaseEnhance_ItemPostDrawInInventory[ 宠物索引 ] != null ) 生效宠物索引_ItemPostDrawInInventory.Add( 宠物索引 );
            if ( System_缓存_静态数据.委托映射_BaseEnhance_ItemUpdateInventory[ 宠物索引 ] != null ) 生效宠物索引_ItemUpdateInventory.Add( 宠物索引 );
        }
    }

}