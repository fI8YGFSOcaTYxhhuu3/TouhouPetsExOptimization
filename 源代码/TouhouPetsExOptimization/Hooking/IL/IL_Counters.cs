using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization.Hooking.IL;



public class IL_Counters : BaseHook {

    private ILHook _ilTile;
    private ILHook _ilNpcAI;
    private ILHook _ilNpcPreAI;
    private ILHook _ilItem;

    public override void Load( Mod targetMod ) {
	
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;

        Type baseEnhance = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );
        if ( baseEnhance == null ) {
            logger.Error( "[IL_Counters] 致命错误：未找到类 TouhouPetsEx.Enhance.Core.BaseEnhance，计数器功能将中止加载。" );
            return;
        }

        Type actionType = typeof( Action<> ).MakeGenericType( baseEnhance );
        Type tileType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceTile" );
        Type npcType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        Type itemType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );

        if ( tileType == null ) logger.Warn( "[IL_Counters] 警告：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceTile" );
        if ( npcType == null ) logger.Warn( "[IL_Counters] 警告：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );
        if ( itemType == null ) logger.Warn( "[IL_Counters] 警告：未找到类 TouhouPetsEx.Enhance.Core.GEnhanceItems" );

        MethodInfo mTileDraw = tileType?.GetMethod( "DrawEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( int ), typeof( int ), typeof( int ), typeof( SpriteBatch ), typeof( TileDrawInfo ).MakeByRefType() ], null );
        if ( mTileDraw != null ) { _ilTile = new ILHook( mTileDraw, il => InjectMethodCallCounter( il, "TileDrawEffects", "调用计数_BaseEnhance_TileDrawEffects" ) ); _ilTile.Apply(); }
        else logger.Warn( "[IL_Counters] 警告：未找到方法 GEnhanceTile.DrawEffects" );

        MethodInfo mNpcAI = npcType?.GetMethod( "AI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( NPC ) ], null );
        if ( mNpcAI != null ) { _ilNpcAI = new ILHook( mNpcAI, il => InjectMethodCallCounter( il, "NPCAI", "调用计数_BaseEnhance_NPCAI" ) ); _ilNpcAI.Apply(); }
        else logger.Warn( "[IL_Counters] 警告：未找到方法 GEnhanceNPCs.AI" );

        MethodInfo mNpcPreAI = npcType?.GetMethod( "PreAI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( NPC ) ], null );
        if ( mNpcPreAI != null ) { _ilNpcPreAI = new ILHook( mNpcPreAI, il => InjectMethodCallCounter( il, "NPCPreAI", "调用计数_BaseEnhance_NPCPreAI" ) ); _ilNpcPreAI.Apply(); }
        else logger.Warn( "[IL_Counters] 警告：未找到方法 GEnhanceNPCs.PreAI" );

        MethodInfo mItem = itemType?.GetMethod( "ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, [ actionType ], null );
        if ( mItem != null ) { _ilItem = new ILHook( mItem, il => InjectDelegateInvokeCounter( il, "调用计数_BaseEnhance_PostDrawInInventory_UpdateInventory" ) ); _ilItem.Apply(); }
        else logger.Warn( "[IL_Counters] 警告：未找到方法 GEnhanceItems.ProcessDemonismAction(Action<BaseEnhance>)" );
    }

    public override void Unload() { _ilTile?.Dispose(); _ilTile = null; _ilNpcAI?.Dispose(); _ilNpcAI = null; _ilNpcPreAI?.Dispose(); _ilNpcPreAI = null; _ilItem?.Dispose(); _ilItem = null; }

    private void InjectMethodCallCounter( ILContext il, string targetMethodName, string counterFieldName ) {
        var c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;
        FieldInfo field = typeof( System_Counter ).GetField( counterFieldName, BindingFlags.Static | BindingFlags.Public );
        if ( field == null ) {
            logger.Error( $"[IL_Counters] 错误：在 System_Counter 中未找到字段 {counterFieldName}" );
            return;
        }

        bool found = false;
        while ( c.TryGotoNext( MoveType.Before, i => i.MatchCallvirt( out var m ) && m.Name == targetMethodName && m.DeclaringType.Name == "BaseEnhance" ) ) {
            found = true;
            EmitIncrement( c, field );
            c.Index++;
        }

        if ( !found ) {
            logger.Warn( $"[IL_Counters] 警告：在 {il.Method.Name} 中未找到对 BaseEnhance.{targetMethodName} 的调用，此处的计数器注入失败。可能是原模组逻辑发生了变更。" );
        }
    }

    private void InjectDelegateInvokeCounter( ILContext il, string counterFieldName ) {
        var c = new ILCursor( il );
        var logger = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;
        FieldInfo field = typeof( System_Counter ).GetField( counterFieldName, BindingFlags.Static | BindingFlags.Public );
        if ( field == null ) {
            logger.Error( $"[IL_Counters] 错误：在 System_Counter 中未找到字段 {counterFieldName}" );
            return;
        }

        bool found = false;
        while ( c.TryGotoNext( MoveType.Before, i => i.MatchCallvirt( out var m ) && m.Name == "Invoke" ) ) {
            found = true;
            EmitIncrement( c, field );
            c.Index++;
        }

        if ( !found ) {
            logger.Warn( $"[IL_Counters] 警告：在 {il.Method.Name} 中未找到委托 Invoke 调用，此处的计数器注入失败。可能是原模组逻辑发生了变更。" );
        }
    }

    private void EmitIncrement( ILCursor c, FieldInfo field ) {
        c.Emit( OpCodes.Ldsfld, field );
        c.Emit( OpCodes.Ldc_I8, 1L );
        c.Emit( OpCodes.Add );
        c.Emit( OpCodes.Stsfld, field );
    }

}