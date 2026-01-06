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
        MethodInfo method = type?.GetMethod( "AI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if ( method != null ) {
            _hook = new ILHook( method, ManipulateIL );
            _hook.Apply();
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );

        c.Goto( 0 );
        c.EmitDelegate<Action>( () => { if ( ModContent.GetInstance<MainConfigs>().性能监控 ) System_Counter.调用计数_GEnhanceNPCs_AI++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs", "ProcessDemonismAction" ) ) ) return;

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate<Func<bool>>( () => { return ModContent.GetInstance<MainConfigs>().优化模式_GEnhanceNPCs_PreAI_AI == MainConfigs.优化模式.关闭补丁; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.EmitDelegate<Action<NPC>>( OptimizedAILoop );
        c.Emit( OpCodes.Br, labelSkipOriginal );
        c.MarkLabel( labelRunOriginal );
        c.Index++;
        c.MarkLabel( labelSkipOriginal );
    }

    private static void OptimizedAILoop( NPC npc ) {
        var config = ModContent.GetInstance<MainConfigs>();

        switch ( config.优化模式_GEnhanceNPCs_PreAI_AI ) {
            case MainConfigs.优化模式.暴力截断: return;
            case MainConfigs.优化模式.智能缓存:
                if ( System_Cache.NpcAI.Count == 0 ) return;

                object[] args = [ npc ];
                foreach ( var tuple in System_Cache.NpcAI ) try {
                        if ( config.性能监控 ) System_Counter.调用计数_BaseEnhance_PreAI_AI++;
                        tuple.Item2.Invoke( tuple.Item1, args );
                    }
                    catch { }

                return;
            default: return;
        }
    }

}