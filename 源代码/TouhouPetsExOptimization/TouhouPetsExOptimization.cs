using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization;



public class TouhouPetsExOptimization : Mod {

    private Hook 钩子_GEnhanceTile_DrawEffects;
    private Hook 钩子_GEnhanceNPCs_AI;
    private Hook 钩子_GEnhanceNPCs_PreAI;
    private Hook 钩子_GEnhanceItems_UpdateInventory;
	
    private ILHook IL钩子_GEnhanceTile_ProcessDemonismAction;
    private ILHook IL钩子_GEnhanceNPCs_ProcessDemonismAction_Action;
    private ILHook IL钩子_GEnhanceNPCs_ProcessDemonismAction_Func;
    private ILHook IL钩子_GEnhanceItems_ProcessDemonismAction;

    private static List<Tuple<object, MethodInfo>> 缓存_GEnhanceTile_DrawEffects = new();
    private static List<Tuple<object, MethodInfo>> 缓存_GEnhanceNPCs_AI = new();
    private static List<Tuple<object, MethodInfo>> 缓存_GEnhanceNPCs_PreAI = new();
    private static List<Tuple<object, MethodInfo>> 缓存_GEnhanceItems_UpdateInventory = new();

    private delegate void 委托_TileDrawEffects( object self, int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData ); 
    private delegate void 委托_TileDrawEffects_钩子( 委托_TileDrawEffects orig, object self, int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData );
    private delegate void 委托_NPCAI( object self, NPC npc );
    private delegate bool 委托_NPCPreAI( object self, NPC npc );
    private delegate void 委托_ItemUpdateInventory( object self, Item item, Player player );

    public override void Load() {
        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod 模组 ) ) return;

        Load_挂钩_GEnhanceTile_DrawEffects( 模组 ); 
        Load_挂钩_GEnhanceNPCs_AI( 模组 );
        Load_挂钩_GEnhanceNPCs_PreAI( 模组 );
        Load_挂钩_GEnhanceItems_UpdateInventory( 模组 );
        Load_挂钩_函数调用计数器( 模组 );
    }

    public override void Unload() {
        钩子_GEnhanceTile_DrawEffects?.Dispose(); 钩子_GEnhanceTile_DrawEffects = null;
        钩子_GEnhanceNPCs_AI?.Dispose(); 钩子_GEnhanceNPCs_AI = null;
        钩子_GEnhanceNPCs_PreAI?.Dispose(); 钩子_GEnhanceNPCs_PreAI = null;
        钩子_GEnhanceItems_UpdateInventory?.Dispose(); 钩子_GEnhanceItems_UpdateInventory = null;
		
        IL钩子_GEnhanceTile_ProcessDemonismAction?.Dispose(); IL钩子_GEnhanceTile_ProcessDemonismAction = null;
        IL钩子_GEnhanceNPCs_ProcessDemonismAction_Action?.Dispose(); IL钩子_GEnhanceNPCs_ProcessDemonismAction_Action = null;
        IL钩子_GEnhanceNPCs_ProcessDemonismAction_Func?.Dispose(); IL钩子_GEnhanceNPCs_ProcessDemonismAction_Func = null;
        IL钩子_GEnhanceItems_ProcessDemonismAction?.Dispose(); IL钩子_GEnhanceItems_ProcessDemonismAction = null;

        缓存_GEnhanceTile_DrawEffects?.Clear(); 缓存_GEnhanceTile_DrawEffects = null;
        缓存_GEnhanceNPCs_AI?.Clear(); 缓存_GEnhanceNPCs_AI = null;
        缓存_GEnhanceNPCs_PreAI?.Clear(); 缓存_GEnhanceNPCs_PreAI = null;
        缓存_GEnhanceItems_UpdateInventory?.Clear(); 缓存_GEnhanceItems_UpdateInventory = null;
    }

    private void Load_挂钩_GEnhanceTile_DrawEffects( Mod 模组 ) {
        Type 类型 = 模组.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceTile" );
        MethodInfo 函数 = 类型?.GetMethod( "DrawEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 函数 == null ) return;

        钩子_GEnhanceTile_DrawEffects = new Hook( 函数, new 委托_TileDrawEffects_钩子( 替换函数_GEnhanceTile_DrawEffects));
        钩子_GEnhanceTile_DrawEffects.Apply();
    }
    
    private static void 替换函数_GEnhanceTile_DrawEffects( 委托_TileDrawEffects orig, object self, int i, int j, int type, SpriteBatch sb, ref TileDrawInfo drawData ) {
        var 模组配置 = ModContent.GetInstance<TouhouPetsExOptimizationConfig>();
        bool 启用性能监控 = 模组配置.性能监控;

        if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_GEnhanceTile_DrawEffects++;
        
        if ( !模组配置.优化开关_GEnhanceTile_DrawEffects ) { orig( self, i, j, type, sb, ref drawData ); return; }
        if ( 模组配置.优化模式_GEnhanceTile_DrawEffects == 优化模式.暴力截断 ) return;
        if ( 缓存_GEnhanceTile_DrawEffects.Count == 0 ) return;

        object[] 参数 = [i, j, type, sb, drawData];
        foreach ( var 元组 in 缓存_GEnhanceTile_DrawEffects ) try {
                if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_BaseEnhance_TileDrawEffects++;
                元组.Item2.Invoke( 元组.Item1, 参数 ); 
            } catch { }

        drawData = ( TileDrawInfo ) 参数[ 4 ];
    }

    private void Load_挂钩_GEnhanceNPCs_AI( Mod 模组 ) {
        Type 类型 = 模组.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        MethodInfo 函数 = 类型?.GetMethod( "AI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 函数 == null ) return;

        钩子_GEnhanceNPCs_AI = new Hook( 函数, new Action<委托_NPCAI, object, NPC>( ( orig, self, npc ) => {
            var 模组配置 = ModContent.GetInstance<TouhouPetsExOptimizationConfig>();
            bool 启用性能监控 = 模组配置.性能监控; 
            
            if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_GEnhanceNPCs_AI++;

            if ( !模组配置.优化开关_GEnhanceNPCs_PreAI_AI ) { orig( self, npc ); return; }
            if ( 模组配置.优化模式_GEnhanceNPCs_PreAI_AI == 优化模式.暴力截断 ) return;
            if ( 缓存_GEnhanceNPCs_AI.Count == 0 ) return;

            object[] 参数 = [npc];
            foreach ( var 元组 in 缓存_GEnhanceNPCs_AI ) try {
                    if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_BaseEnhance_PreAI_AI++;
                    元组.Item2.Invoke( 元组.Item1, 参数 ); 
                } catch { }
        } ) );
        钩子_GEnhanceNPCs_AI.Apply();
    }

    private void Load_挂钩_GEnhanceNPCs_PreAI( Mod 模组 ) {
        Type 类型 = 模组.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        MethodInfo 函数 = 类型?.GetMethod( "PreAI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 函数 == null ) return;

        钩子_GEnhanceNPCs_PreAI = new Hook( 函数, new Func<委托_NPCPreAI, object, NPC, bool>( ( orig, self, npc ) => {
            var 模组配置 = ModContent.GetInstance<TouhouPetsExOptimizationConfig>(); 
            bool 启用性能监控 = 模组配置.性能监控; 
            
            if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_GEnhanceNPCs_PreAI++;

            if ( !模组配置.优化开关_GEnhanceNPCs_PreAI_AI ) return orig( self, npc );
            if ( 模组配置.优化模式_GEnhanceNPCs_PreAI_AI == 优化模式.暴力截断 ) return true;
            if ( 缓存_GEnhanceNPCs_PreAI.Count == 0 ) return true;

            object[] 参数 = [npc];
            bool? 最终结果 = null;

            foreach ( var 元组 in 缓存_GEnhanceNPCs_PreAI ) {
                try {
                    if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_BaseEnhance_PreAI_AI++;

                    bool? 结果 = ( bool? ) 元组.Item2.Invoke( 元组.Item1, 参数 );
                    if ( 结果 == false ) return false;
                    if ( 结果 != null ) 最终结果 = 结果;
                }
                catch { }
            }
            return 最终结果 ?? true;
        } ) );
        钩子_GEnhanceNPCs_PreAI.Apply();
    }

    private void Load_挂钩_GEnhanceItems_UpdateInventory( Mod 模组 ) {
        Type 类型 = 模组.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );
        MethodInfo 函数 = 类型?.GetMethod( "UpdateInventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 函数 == null ) return;

        钩子_GEnhanceItems_UpdateInventory = new Hook( 函数, new Action<委托_ItemUpdateInventory, object, Item, Player>( ( orig, self, item, player ) => {
            var 模组配置 = ModContent.GetInstance<TouhouPetsExOptimizationConfig>();
            bool 启用性能监控 = 模组配置.性能监控;

            if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_GEnhanceItems_UpdateInventory++;
            
            if ( !模组配置.优化开关_GEnhanceItems_UpdateInventory ) { orig( self, item, player ); return; }
            if ( 模组配置.优化模式_GEnhanceItems_UpdateInventory == 优化模式.暴力截断 ) return;

            if ( item.ModItem?.Mod.Name == "TouhouPets" ) { orig( self, item, player ); return; }
            if ( 缓存_GEnhanceItems_UpdateInventory.Count == 0 ) return;

            object[] 参数 = [item, player];
            foreach ( var 元组 in 缓存_GEnhanceItems_UpdateInventory ) try { 
                if ( 启用性能监控 ) TouhouPetsExOptimizationDebugSystem.调用计数_BaseEnhance_UpdateInventory++;
                元组.Item2.Invoke( 元组.Item1, 参数 ); 
            } catch { }
        } ) );
        钩子_GEnhanceItems_UpdateInventory.Apply();
    }

    private void Load_挂钩_函数调用计数器(Mod 模组) {
        Type 类型_BaseEnhance = 模组.Code.GetType("TouhouPetsEx.Enhance.Core.BaseEnhance");
        Type 类型_Action = typeof(Action<>).MakeGenericType(类型_BaseEnhance);
        Type 类型_Func = typeof(Func<,>).MakeGenericType(类型_BaseEnhance, typeof(bool?));

        Type 类型_Tile = 模组.Code.GetType("TouhouPetsEx.Enhance.Core.GEnhanceTile");
        MethodInfo 方法_Tile = 类型_Tile?.GetMethod("ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { 类型_Action }, null);
        if (方法_Tile != null) {
            IL钩子_GEnhanceTile_ProcessDemonismAction = new ILHook(方法_Tile, (il) => 注入计数逻辑(il, "调用计数_BaseEnhance_TileDrawEffects"));
            IL钩子_GEnhanceTile_ProcessDemonismAction.Apply();
        }

        Type 类型_NPC = 模组.Code.GetType("TouhouPetsEx.Enhance.Core.GEnhanceNPCs");
        MethodInfo 方法_NPC_Action = 类型_NPC?.GetMethod("ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { 类型_Action }, null);
        if (方法_NPC_Action != null) {
            IL钩子_GEnhanceNPCs_ProcessDemonismAction_Action = new ILHook(方法_NPC_Action, (il) => 注入计数逻辑(il, "调用计数_BaseEnhance_PreAI_AI" ) );
            IL钩子_GEnhanceNPCs_ProcessDemonismAction_Action.Apply();
        }

        MethodInfo 方法_NPC_Func = 类型_NPC?.GetMethod("ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(bool?), 类型_Func }, null);
        if (方法_NPC_Func != null) {
            IL钩子_GEnhanceNPCs_ProcessDemonismAction_Func = new ILHook(方法_NPC_Func, (il) => 注入计数逻辑(il, "调用计数_BaseEnhance_PreAI_AI" ) );
            IL钩子_GEnhanceNPCs_ProcessDemonismAction_Func.Apply();
        }

        Type 类型_Item = 模组.Code.GetType("TouhouPetsEx.Enhance.Core.GEnhanceItems");
        MethodInfo 方法_Item = 类型_Item?.GetMethod("ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { 类型_Action }, null);
        if (方法_Item != null) {
            IL钩子_GEnhanceItems_ProcessDemonismAction = new ILHook(方法_Item, (il) => 注入计数逻辑(il, "调用计数_BaseEnhance_UpdateInventory"));
            IL钩子_GEnhanceItems_ProcessDemonismAction.Apply();
        }
    }

    private void 注入计数逻辑( ILContext il, string 计数器字段名 ) {
        var c = new ILCursor( il );
        FieldInfo 字段 = typeof( TouhouPetsExOptimizationDebugSystem ).GetField( 计数器字段名, BindingFlags.Static | BindingFlags.Public );

        if ( 字段 == null ) return;

        while ( c.TryGotoNext( MoveType.Before, i => i.MatchCallvirt( out var m ) && m.Name == "Invoke" ) ) {
            c.Emit( Mono.Cecil.Cil.OpCodes.Ldsfld, 字段 );
            c.Emit( Mono.Cecil.Cil.OpCodes.Ldc_I8, 1L );
            c.Emit( Mono.Cecil.Cil.OpCodes.Add );
            c.Emit( Mono.Cecil.Cil.OpCodes.Stsfld, 字段 );

            c.Index++;
        }
    }

    public override void PostSetupContent() { PostSetupContent_构建缓存(); }

    private static void PostSetupContent_构建缓存() {
        缓存_GEnhanceTile_DrawEffects.Clear();
        缓存_GEnhanceNPCs_AI.Clear();
        缓存_GEnhanceNPCs_PreAI.Clear();
        缓存_GEnhanceItems_UpdateInventory.Clear();

        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod 模组 ) ) return;

        Type 类型_TouhouPetsEx = 模组.Code.GetType( "TouhouPetsEx.TouhouPetsEx" );
        Type 类型_BaseEnhance = 模组.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );

        FieldInfo 字段_GEnhanceInstances = 类型_TouhouPetsEx?.GetField( "GEnhanceInstances", BindingFlags.Static | BindingFlags.Public );
        if ( 字段_GEnhanceInstances == null ) return;

        object 对象_GEnhanceInstances = 字段_GEnhanceInstances.GetValue( null );
        if ( 对象_GEnhanceInstances == null ) return;

        var 宠物集合 = 对象_GEnhanceInstances.GetType().GetProperty( "Values" ).GetValue( 对象_GEnhanceInstances ) as System.Collections.IEnumerable;
        if ( 宠物集合 == null ) return;

        foreach ( object 宠物 in 宠物集合 ) {
            if ( 宠物 == null ) continue;
            Type 宠物类型 = 宠物.GetType();

            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "TileDrawEffects", 缓存_GEnhanceTile_DrawEffects );
            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "NPCAI", 缓存_GEnhanceNPCs_AI );
            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "NPCPreAI", 缓存_GEnhanceNPCs_PreAI );
            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "ItemUpdateInventory", 缓存_GEnhanceItems_UpdateInventory );
        }
    }

    private static void 注册缓存( object 实例, Type 实例类型, Type 基类类型, string 方法名, List<Tuple<object, MethodInfo>> 缓存列表 ) {
        MethodInfo 方法 = 实例类型.GetMethod( 方法名, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 方法 != null && 方法.DeclaringType != 基类类型 ) 缓存列表.Add( new Tuple<object, MethodInfo>( 实例, 方法 ) );
    }

}