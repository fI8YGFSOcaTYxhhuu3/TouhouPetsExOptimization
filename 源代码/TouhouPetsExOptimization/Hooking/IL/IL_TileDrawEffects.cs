using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Configs;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization.Hooking.IL;



public class IL_TileDrawEffects : BaseHook {

    private ILHook _hook;

    private delegate void TileDrawEffectsDelegate( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData );

    public override void Load( Mod targetMod ) {
        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceTile" );
        MethodInfo method = type?.GetMethod( "DrawEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if ( method != null ) {
            _hook = new ILHook( method, ManipulateIL );
            _hook.Apply();
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );

        c.Goto( 0 );
        c.EmitDelegate<Action>( () => { if ( ModContent.GetInstance<MainConfigs>().性能监控 ) System_Counter.调用计数_GEnhanceTile_DrawEffects++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceTile", "ProcessDemonismAction" ) ) ) return;

        ILLabel labelRunOriginal = c.DefineLabel();
        ILLabel labelSkipOriginal = c.DefineLabel();

        c.EmitDelegate<Func<bool>>( () => { return ModContent.GetInstance<MainConfigs>().优化模式_GEnhanceTile_DrawEffects == MainConfigs.优化模式.关闭补丁; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.Emit( OpCodes.Ldarg_2 );
        c.Emit( OpCodes.Ldarg_3 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 4 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 5 );
        c.EmitDelegate<TileDrawEffectsDelegate>( OptimizedDrawEffectsLoop );
        c.Emit( OpCodes.Ret );
        c.MarkLabel( labelRunOriginal );
        c.Index++;
        c.MarkLabel( labelSkipOriginal );
    }

    private static void OptimizedDrawEffectsLoop( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData ) {
        var config = ModContent.GetInstance<MainConfigs>();

        switch ( config.优化模式_GEnhanceTile_DrawEffects ) {
            case MainConfigs.优化模式.暴力截断: return;
            case MainConfigs.优化模式.智能缓存:
                if ( System_Cache.TileDrawEffects.Count == 0 ) return;

                object[] args = [ i, j, type, spriteBatch, drawData ];
                foreach ( var tuple in System_Cache.TileDrawEffects ) try {
                        if ( config.性能监控 ) System_Counter.调用计数_BaseEnhance_TileDrawEffects++;
                        tuple.Item2.Invoke( tuple.Item1, args );
                    }
                    catch { }
                drawData = ( TileDrawInfo ) args[ 4 ];

                return;
            default: return;
        }
    }

}