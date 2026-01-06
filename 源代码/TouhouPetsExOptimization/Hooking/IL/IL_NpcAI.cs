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



public class IL_NpcAI : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        MethodInfo method = type?.GetMethod( "AI",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof( NPC )],
            null
        );
        if ( method != null ) { _hook = new ILHook( method, ManipulateIL ); _hook.Apply(); }
		
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceNPCs_AI++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs", "ProcessDemonismAction" ) ) ) return;

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceNPCs_PreAI_AI == MainConfigs.优化模式.关闭补丁; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.EmitDelegate( OptimizedCode );
        c.Emit( OpCodes.Br, labelSkipOriginal );
        c.MarkLabel( labelRunOriginal );
        c.Index++; 
        c.MarkLabel( labelSkipOriginal );
    }

    private static void OptimizedCode( NPC npc ) {
        switch ( MainConfigCache.优化模式_GEnhanceNPCs_PreAI_AI ) {
            case MainConfigs.优化模式.暴力截断 or MainConfigs.优化模式.旧版模拟: return;
            case MainConfigs.优化模式.智能缓存:
                var validPets = System_State.LocalPlayerActivePets;

                for ( int i = 0; i < validPets.Count; i++ ) {
                    var action = System_Cache.Dispatch_BaseEnhance_NPCAI[ validPets[ i ] ];
                    if ( action != null ) {
                        if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_PreAI_AI++;
                        action( npc );
                    }
                }

                return;
            default: return;
        }
    }

}