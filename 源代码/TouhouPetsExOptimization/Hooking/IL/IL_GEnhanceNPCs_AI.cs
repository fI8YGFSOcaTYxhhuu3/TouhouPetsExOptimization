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



public class IL_GEnhanceNPCs_AI : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        if ( type == null ) {
            logger.Warn( "[IL_GEnhanceNPCs_AI] 警告：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceNPCs，此补丁将跳过加载。" );
            return;
        }

        MethodInfo method = type.GetMethod( "AI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( NPC ) ], null );
        if ( method != null ) { 
            _hook = new ILHook( method, ManipulateIL ); 
            _hook.Apply(); 
        }
        else {
            logger.Warn( "[IL_GEnhanceNPCs_AI] 警告：未找到方法 GEnhanceNPCs.AI(NPC)，此补丁无法应用。" );
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceNPCs_AI++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchLdsfld( "TouhouPetsEx.Enhance.Core.EnhanceHookRegistry", "NPCAI" ) ) ) {
            logger.Warn( $"[IL_GEnhanceNPCs_AI] 警告：在 {il.Method.Name} 中未找到对 EnhanceHookRegistry.NPCAI 的访问，优化补丁注入失败。可能是原模组逻辑发生了变更。" );
            return;
        }

        ILLabel labelRunOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceNPCs_AI == MainConfigs.优化模式.关闭补丁 || !System_PatchState.IsSafeToOptimize; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Ldarg_1 );
        c.EmitDelegate( OptimizedCode );
        c.Emit( OpCodes.Ret );
        c.MarkLabel( labelRunOriginal );
    }

    private static void OptimizedCode( NPC npc ) {
        switch ( MainConfigCache.优化模式_GEnhanceNPCs_AI ) {
            case MainConfigs.优化模式.暴力截断 or MainConfigs.优化模式.旧版模拟: return;
            case MainConfigs.优化模式.智能缓存:
                var activeIndices = System_State.ActiveEnhanceIndices;
                var actions = System_Cache.Actions_BaseEnhance_NPCAI;

                int count = activeIndices.Count;
                for ( int i = 0; i < count; i++ ) {
                    var action = actions[ activeIndices[ i ] ];
                    if ( action != null ) {
                        if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_NPCAI++;
                        action( npc );
                    }
                }

                return;
            default: return;
        }
    }

}