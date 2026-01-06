using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization.Hooking.IL;



public class IL_Counters : BaseHook {

    private ILHook _ilTile;
    private ILHook _ilNpcAction;
    private ILHook _ilNpcFunc;
    private ILHook _ilItem;

    public override void Load( Mod targetMod ) {
        Type baseEnhance = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );
        if ( baseEnhance == null ) return;

        Type actionType = typeof( Action<> ).MakeGenericType( baseEnhance );
        Type funcType = typeof( Func<,> ).MakeGenericType( baseEnhance, typeof( bool? ) );

        Type tileType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceTile" );
        MethodInfo mTile = tileType?.GetMethod( "ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, [actionType], null );
        if ( mTile != null ) { _ilTile = new ILHook( mTile, il => InjectCounter( il, "调用计数_BaseEnhance_TileDrawEffects" ) ); _ilTile.Apply(); }

        Type npcType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceNPCs" );

        MethodInfo mNpcAction = npcType?.GetMethod( "ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, [actionType], null );
        if ( mNpcAction != null ) { _ilNpcAction = new ILHook( mNpcAction, il => InjectCounter( il, "调用计数_BaseEnhance_PreAI_AI" ) ); _ilNpcAction.Apply(); }

        MethodInfo mNpcFunc = npcType?.GetMethod( "ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, [typeof( bool? ), funcType], null );
        if ( mNpcFunc != null ) { _ilNpcFunc = new ILHook( mNpcFunc, il => InjectCounter( il, "调用计数_BaseEnhance_PreAI_AI" ) ); _ilNpcFunc.Apply(); }

        Type itemType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.GEnhanceItems" );
        MethodInfo mItem = itemType?.GetMethod( "ProcessDemonismAction", BindingFlags.Static | BindingFlags.NonPublic, null, [actionType], null );
        if ( mItem != null ) { _ilItem = new ILHook( mItem, il => InjectCounter( il, "调用计数_BaseEnhance_UpdateInventory" ) ); _ilItem.Apply(); }
    }

    public override void Unload() { _ilTile?.Dispose(); _ilTile = null; _ilNpcAction?.Dispose(); _ilNpcAction = null; _ilNpcFunc?.Dispose(); _ilNpcFunc = null; _ilItem?.Dispose(); _ilItem = null; }

    private void InjectCounter( ILContext il, string fieldName ) {
        var c = new ILCursor( il );
        FieldInfo field = typeof( System_Counter ).GetField( fieldName, BindingFlags.Static | BindingFlags.Public );

        if ( field == null ) return;

        while ( c.TryGotoNext( MoveType.Before, i => i.MatchCallvirt( out var m ) && m.Name == "Invoke" ) ) {
            c.Emit( Mono.Cecil.Cil.OpCodes.Ldsfld, field );
            c.Emit( Mono.Cecil.Cil.OpCodes.Ldc_I8, 1L );
            c.Emit( Mono.Cecil.Cil.OpCodes.Add );
            c.Emit( Mono.Cecil.Cil.OpCodes.Stsfld, field );
            c.Index++;
        }
    }

}