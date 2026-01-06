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



public class IL_NpcPreAI : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        MethodInfo method = type?.GetMethod( "PreAI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if ( method != null ) {
            _hook = new ILHook( method, ManipulateIL );
            _hook.Apply();
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );

        c.Goto( 0 );
        c.EmitDelegate<Action>( () => { if ( ModContent.GetInstance<MainConfigs>().性能监控 ) System_Counter.调用计数_GEnhanceNPCs_PreAI++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs", "ProcessDemonismAction" ) ) ) return;

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate<Func<bool>>( () => { return ModContent.GetInstance<MainConfigs>().优化模式_GEnhanceNPCs_PreAI_AI == MainConfigs.优化模式.关闭补丁; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.EmitDelegate<Func<NPC, bool?>>( OptimizedPreAILoop );
        c.Emit( OpCodes.Br, labelSkipOriginal );
        c.MarkLabel( labelRunOriginal );
        c.Index++;
        c.MarkLabel( labelSkipOriginal );
    }

    private static bool? OptimizedPreAILoop( NPC npc ) {
        var config = ModContent.GetInstance<MainConfigs>();

        switch ( config.优化模式_GEnhanceNPCs_PreAI_AI ) {
            case MainConfigs.优化模式.暴力截断: return null;
            case MainConfigs.优化模式.智能缓存:
                if ( System_Cache.NpcPreAI.Count == 0 ) return null;

                object[] args = [ npc ];
                bool? finalResult = null;
                foreach ( var tuple in System_Cache.NpcPreAI ) try {
                        if ( config.性能监控 ) System_Counter.调用计数_BaseEnhance_PreAI_AI++;

                        bool? res = ( bool? ) tuple.Item2.Invoke( tuple.Item1, args );

                        if ( res == false ) return false;
                        if ( res != null ) finalResult = res;
                    }
                    catch { }

                return finalResult;
            default: return null;
        }
    }

}