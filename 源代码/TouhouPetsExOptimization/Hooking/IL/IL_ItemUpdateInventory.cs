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
        MethodInfo method = type?.GetMethod( "UpdateInventory",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [ typeof( Item ), typeof( Player ) ],
            null
        );
        if ( method != null ) { _hook = new ILHook( method, ManipulateIL ); _hook.Apply(); }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceItems_UpdateInventory++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceItems", "ProcessDemonismAction" ) ) ) return;

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceItems_UpdateInventory == MainConfigs.优化模式.关闭补丁; } );

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
            case MainConfigs.优化模式.暴力截断: return;
            case MainConfigs.优化模式.智能缓存:
                var activePets = System_State.LocalPlayerActivePets;

                for ( int i = 0; i < activePets.Count; i++ ) {
                    var action = System_Cache.Dispatch_BaseEnhance_ItemUpdateInventory[ activePets[ i ] ];
                    if ( action != null ) {
                        if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_UpdateInventory++;
                        action( item, player );
                    }
                }

                return;
            default: return;
        }
    }

}