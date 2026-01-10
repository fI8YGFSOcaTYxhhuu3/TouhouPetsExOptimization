using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Configs;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization.Hooking.IL;



public class IL_GEnhanceItems_UpdateInventory : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );
        if ( type == null ) {
            logger.Error( "[IL_GEnhanceItems_UpdateInventory] 致命错误：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceItems，补丁无法加载。" );
            return;
        }

        MethodInfo method = type.GetMethod( "UpdateInventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( Item ), typeof( Player ) ], null );
        if ( method != null ) { 
            _hook = new ILHook( method, ManipulateIL ); 
            _hook.Apply(); 
        }
        else {
            logger.Error( "[IL_GEnhanceItems_UpdateInventory] 致命错误：未找到方法 GEnhanceItems.UpdateInventory(Item, Player)，补丁无法加载。" );
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceItems_UpdateInventory++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceItems", "ProcessDemonismAction" ) ) ) {
            logger.Warn( $"[IL_GEnhanceItems_UpdateInventory] 警告：在 {il.Method.Name} 中未找到对 ProcessDemonismAction 的调用，优化补丁未生效。可能是原模组逻辑发生了变更。" );
            return;
        }

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceItems_UpdateInventory == MainConfigs.优化模式.关闭补丁 || !System_补丁自检.未发生已知错误; } );

        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.Emit( OpCodes.Ldarg_2 );
        c.EmitDelegate( OptimizedCode );
        c.Emit( OpCodes.Br, labelSkipOriginal );
        c.MarkLabel( labelRunOriginal );
        c.Index++;
        c.MarkLabel( labelSkipOriginal );
    }

    private static void OptimizedCode( Item item, Player player ) {
        switch ( MainConfigCache.优化模式_GEnhanceItems_UpdateInventory ) {
            case MainConfigs.优化模式.暴力截断 or MainConfigs.优化模式.旧版模拟: return;
            case MainConfigs.优化模式.智能缓存:
                var activeIndices = System.Runtime.InteropServices.CollectionsMarshal.AsSpan( System_缓存_动态数据.生效宠物索引_ItemUpdateInventory );
                var actions = System_缓存_静态数据.委托映射_BaseEnhance_ItemUpdateInventory;
                int selfIndex = System_缓存_静态数据.宠物索引映射_物品ID[ item.type ];

                for ( int i = 0; i < activeIndices.Length; i++ ) {
                    if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_PostDrawInInventory_UpdateInventory++;
                    int index = activeIndices[ i ];
                    if ( index == selfIndex ) selfIndex = -1;
                    actions[ index ]( item, player );
                }

                if ( selfIndex != -1 ) {
                    var selfAction = actions[ selfIndex ]; if ( selfAction == null ) return;
                    if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_PostDrawInInventory_UpdateInventory++;
                    selfAction( item, player );
                }

                return;
            default: return;
        }
    }

}