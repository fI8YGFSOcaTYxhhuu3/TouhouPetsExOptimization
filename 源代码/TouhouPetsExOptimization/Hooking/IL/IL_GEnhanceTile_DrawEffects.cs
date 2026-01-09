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



public class IL_GEnhanceTile_DrawEffects : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceTile" );
        if ( type == null ) {
            logger.Error( "[IL_GEnhanceTile_DrawEffects] 致命错误：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceTile，此优化模块将无法加载。" );
            return;
        }

        MethodInfo method = type.GetMethod( "DrawEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( int ), typeof( int ), typeof( int ), typeof( SpriteBatch ), typeof( TileDrawInfo ).MakeByRefType() ], null );
        if ( method != null ) { 
            _hook = new ILHook( method, ManipulateIL ); 
            _hook.Apply(); 
        } 
        else {
            logger.Error( "[IL_GEnhanceTile_DrawEffects] 致命错误：未找到方法 GEnhanceTile.DrawEffects(int, int, int, SpriteBatch, ref TileDrawInfo)，此优化模块将无法加载。" );
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceTile_DrawEffects++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchLdsfld( "TouhouPetsEx.Enhance.Core.EnhanceHookRegistry", "TileDrawEffects" ) ) ) {
            logger.Warn( $"[IL_GEnhanceTile_DrawEffects] 警告：在 {il.Method.Name} 中未找到字段引用 EnhanceHookRegistry.TileDrawEffects，IL 注入失败。原模组逻辑可能已变更。" );
            return;
        }

        ILLabel labelRunOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceTile_DrawEffects == MainConfigs.优化模式.关闭补丁 || !System_PatchState.IsSafeToOptimize; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Ldarg_1 );
        c.Emit( OpCodes.Ldarg_2 );
        c.Emit( OpCodes.Ldarg_3 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 4 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 5 );
        c.EmitDelegate( OptimizedCode );
        c.Emit( OpCodes.Ret );
        c.MarkLabel( labelRunOriginal );
    }

    private static void OptimizedCode( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData ) {
        switch ( MainConfigCache.优化模式_GEnhanceTile_DrawEffects ) {
            case MainConfigs.优化模式.暴力截断 or MainConfigs.优化模式.旧版模拟: return;
            case MainConfigs.优化模式.智能缓存:
                var activeIndices = System.Runtime.InteropServices.CollectionsMarshal.AsSpan( System_State.ActiveIndices_TileDrawEffects );
                var actions = System_Cache.Actions_BaseEnhance_TileDrawEffects;

                for ( int index = 0; index < activeIndices.Length; index++ ) {
                    if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_TileDrawEffects++;
                    actions[ activeIndices[ index ] ]( i, j, type, spriteBatch, ref drawData );
                }

                return;
            default: return;
        }
    }

}