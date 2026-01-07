using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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



public class IL_GEnhanceItems_PostDrawInInventory : BaseHook {

    private ILHook _hook;

    public override void Load( Mod targetMod ) {
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        Type type = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );
        if ( type == null ) {
            logger.Error( "[IL_GEnhanceItems_PostDrawInInventory] 致命错误：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceItems，优化将跳过。" );
            return;
        }

        MethodInfo method = type.GetMethod( "PostDrawInInventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( Item ), typeof( SpriteBatch ), typeof( Vector2 ), typeof( Rectangle ), typeof( Color ), typeof( Color ), typeof( Vector2 ), typeof( float ) ], null );
        
        if ( method != null ) { 
            _hook = new ILHook( method, ManipulateIL ); 
            _hook.Apply(); 
        }
        else {
            logger.Warn( "[IL_GEnhanceItems_PostDrawInInventory] 警告：未找到方法 GEnhanceItems.PostDrawInInventory" );
        }
    }

    public override void Unload() { _hook?.Dispose(); _hook = null; }

    private void ManipulateIL( ILContext il ) {
        ILCursor c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        c.Goto( 0 );
        c.EmitDelegate( () => { if ( MainConfigCache.性能监控 ) System_Counter.调用计数_GEnhanceItems_PostDrawInInventory++; } );

        if ( !c.TryGotoNext( MoveType.Before, i => i.MatchCall( "TouhouPetsEx.Enhance.Core.GEnhanceItems", "ProcessDemonismAction" ) ) ) {
            logger.Warn( $"[IL_GEnhanceItems_PostDrawInInventory] 警告：在 {il.Method.Name} 中未找到目标调用 ProcessDemonismAction，优化注入失败。可能是原模组逻辑发生了变更。" );
            return;
        }

        ILLabel labelRunOriginal = c.DefineLabel();

        c.EmitDelegate( () => { return MainConfigCache.优化模式_GEnhanceItems_UpdateInventory == MainConfigs.优化模式.关闭补丁 || !System_PatchState.IsSafeToOptimize; } );
        c.Emit( OpCodes.Brtrue, labelRunOriginal );
        c.Emit( OpCodes.Pop );
        c.Emit( OpCodes.Ldarg_1 );
        c.Emit( OpCodes.Ldarg_2 );
        c.Emit( OpCodes.Ldarg_3 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 4 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 5 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 6 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 7 );
        c.Emit( OpCodes.Ldarg_S, ( byte ) 8 );
        c.EmitDelegate( OptimizedCode );
        c.Emit( OpCodes.Ret );
        c.MarkLabel( labelRunOriginal );
        c.Index++;
    }

    private static void OptimizedCode( Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale ) {
        switch ( MainConfigCache.优化模式_GEnhanceItems_UpdateInventory ) {
            case MainConfigs.优化模式.暴力截断 or MainConfigs.优化模式.旧版模拟: return;
            case MainConfigs.优化模式.智能缓存:
                var activePets = System_State.LocalPlayerActivePets;

                for ( int i = 0; i < activePets.Count; i++ ) {
                    var action = System_Cache.Dispatch_BaseEnhance_ItemPostDrawInInventory[ activePets[ i ] ];
                    if ( action != null ) {
                        if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_PostDrawInInventory_UpdateInventory++;
                        action( item, spriteBatch, position, frame, drawColor, itemColor, origin, scale );
                    }
                }

                return;
            default: return;
        }
    }

}