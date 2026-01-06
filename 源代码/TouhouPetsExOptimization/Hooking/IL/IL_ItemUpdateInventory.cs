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



public class IL_ItemUpdateInventory : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );
        MethodInfo method = type?.GetMethod( "UpdateInventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if ( method != null ) {
            _hook = new ILHook( method, ManipulateIL );
            _hook.Apply();
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );

        c.Goto( 0 );
        c.EmitDelegate<Action>( () => { if ( ModContent.GetInstance<MainConfigs>().性能监控 ) System_Counter.调用计数_GEnhanceItems_UpdateInventory++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceItems", "ProcessDemonismAction" ) ) ) return;

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate<Func<bool>>( () => { return ModContent.GetInstance<MainConfigs>().优化模式_GEnhanceItems_UpdateInventory == MainConfigs.优化模式.关闭补丁; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.Emit( OpCodes.Ldarg_2 );
        c.EmitDelegate<Action<Item, Player>>( RunOptimizedLogic );
        c.Emit( OpCodes.Br, labelSkipOriginal );
        c.MarkLabel( labelRunOriginal );
        c.Index++;
        c.MarkLabel( labelSkipOriginal );
    }

    private static void RunOptimizedLogic( Item item, Player player ) {
        var config = ModContent.GetInstance<MainConfigs>();

        switch ( config.优化模式_GEnhanceItems_UpdateInventory ) {
            case MainConfigs.优化模式.暴力截断: return;
            case MainConfigs.优化模式.智能缓存:
                if ( System_Cache.ItemUpdateInventory.Count == 0 ) return;

                object[] args = [ item, player ];
                foreach ( var tuple in System_Cache.ItemUpdateInventory ) try {
                        if ( config.性能监控 ) System_Counter.调用计数_BaseEnhance_UpdateInventory++;
                        tuple.Item2.Invoke( tuple.Item1, args );
                    }
                    catch { }

                return;
            default: return;
        }
    }

}