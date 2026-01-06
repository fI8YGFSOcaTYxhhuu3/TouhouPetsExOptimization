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
        Type baseEnhance = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );
        if ( baseEnhance == null ) return;

        Type tileType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceTile" );

        MethodInfo mTileDraw = tileType?.GetMethod( "DrawEffects",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [ typeof( int ), typeof( int ), typeof( int ), typeof( SpriteBatch ), typeof( TileDrawInfo ).MakeByRefType() ],
            null
        );

        if ( mTileDraw != null ) { _ilTile = new ILHook( mTileDraw, il => InjectMethodCallCounter( il, "TileDrawEffects", "调用计数_BaseEnhance_TileDrawEffects" ) ); _ilTile.Apply(); }

        Type npcType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );

        MethodInfo mNpcPreAI = npcType?.GetMethod( "PreAI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( NPC ) ], null );
        if ( mNpcPreAI != null ) { _ilNpcPreAI = new ILHook( mNpcPreAI, il => InjectMethodCallCounter( il, "NPCPreAI", "调用计数_BaseEnhance_PreAI_AI" ) ); _ilNpcPreAI.Apply(); }

        MethodInfo mNpcAI = npcType?.GetMethod( "AI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [ typeof( NPC ) ], null );
        if ( mNpcAI != null ) { _ilNpcAI = new ILHook( mNpcAI, il => InjectMethodCallCounter( il, "NPCAI", "调用计数_BaseEnhance_PreAI_AI" ) ); _ilNpcAI.Apply(); }

        Type itemType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );
        Type actionType = typeof( Action<> ).MakeGenericType( baseEnhance );
        MethodInfo mItem = itemType?.GetMethod( "ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, [ actionType ], null );
        if ( mItem != null ) { _ilItem = new ILHook( mItem, il => InjectDelegateInvokeCounter( il, "调用计数_BaseEnhance_UpdateInventory" ) ); _ilItem.Apply(); }
    }

    public override void Unload() { _ilTile?.Dispose(); _ilTile = null; _ilNpcAI?.Dispose(); _ilNpcAI = null; _ilNpcPreAI?.Dispose(); _ilNpcPreAI = null; _ilItem?.Dispose(); _ilItem = null; }

    private void InjectMethodCallCounter( ILContext il, string targetMethodName, string counterFieldName ) {
        var c = new ILCursor( il );
        FieldInfo field = typeof( System_Counter ).GetField( counterFieldName, BindingFlags.Static | BindingFlags.Public );
        if ( field == null ) return;

        while ( c.TryGotoNext( MoveType.Before, i => i.MatchCallvirt( out var m ) && m.Name == targetMethodName && m.DeclaringType.Name == "BaseEnhance" ) ) {
            EmitIncrement( c, field );
            c.Index++;
        }
    }

    private void InjectDelegateInvokeCounter( ILContext il, string counterFieldName ) {
        var c = new ILCursor( il );
        FieldInfo field = typeof( System_Counter ).GetField( counterFieldName, BindingFlags.Static | BindingFlags.Public );
        if ( field == null ) return;

        while ( c.TryGotoNext( MoveType.Before, i => i.MatchCallvirt( out var m ) && m.Name == "Invoke" ) ) {
            EmitIncrement( c, field );
            c.Index++;
        }
    }

    private void EmitIncrement( ILCursor c, FieldInfo field ) {
        c.Emit( OpCodes.Ldsfld, field );
        c.Emit( OpCodes.Ldc_I8, 1L );
        c.Emit( OpCodes.Add );
        c.Emit( OpCodes.Stsfld, field );
    }

}