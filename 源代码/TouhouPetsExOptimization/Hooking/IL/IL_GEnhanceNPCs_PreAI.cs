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



public class IL_GEnhanceNPCs_PreAI : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        if ( type == null ) {
            logger.Warn( "[IL_GEnhanceNPCs_PreAI] 警告：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
            return;
        }

        MethodInfo method = type.GetMethod( "PreAI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( NPC ) ], null );
        if ( method != null ) { 
            _hook = new ILHook( method, ManipulateIL ); 
            _hook.Apply(); 
        }
        else {
            logger.Warn( "[IL_GEnhanceNPCs_PreAI] 警告：未找到方法 GEnhanceNPCs.PreAI" );
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceNPCs_PreAI++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchLdsfld( "TouhouPetsEx.Enhance.Core.EnhanceHookRegistry", "NPCPreAI" ) ) ) {
            logger.Warn( $"[IL_GEnhanceNPCs_PreAI] 警告：在 {il.Method.Name} 中未找到对 EnhanceHookRegistry.NPCPreAI 的字段读取。优化注入失败，原模组逻辑可能已变更。" );
            return;
        }

        ILLabel labelRunOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceNPCs_PreAI == MainConfigs.优化模式.关闭补丁 || !System_补丁自检.未发生已知错误; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Ldarg_1 );
        c.EmitDelegate( OptimizedCode );
        c.Emit( OpCodes.Ret );
        c.MarkLabel( labelRunOriginal );
    }

    private static bool OptimizedCode( NPC npc ) {
        switch ( MainConfigCache.优化模式_GEnhanceNPCs_PreAI ) {
            case MainConfigs.优化模式.暴力截断 or MainConfigs.优化模式.旧版模拟: return true;
            case MainConfigs.优化模式.智能缓存:
                var activeIndices = System.Runtime.InteropServices.CollectionsMarshal.AsSpan( System_缓存_动态数据.生效宠物索引_NPCPreAI );
                var actions = System_缓存_静态数据.委托映射_BaseEnhance_NPCPreAI;
                bool? finalResult = null;

                for ( int i = 0; i < activeIndices.Length; i++ ) {
                    if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_NPCPreAI++;
                    bool? result = actions[ activeIndices[ i ] ]( npc );
                    if ( result == false ) return false;
                    if ( result != null ) finalResult = result;
                }

                return finalResult ?? true;
            default: return true;
        }
    }

}