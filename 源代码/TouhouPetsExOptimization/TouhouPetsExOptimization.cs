using System.Collections.Generic;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Hooking;
using TouhouPetsExOptimization.Hooking.IL;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization;



public class TouhouPetsExOptimization : Mod {

    private List<BaseHook> _hooks = new();

    public override void Load() {
        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) return;

        _hooks.Add( new IL_TileDrawEffects() );
        _hooks.Add( new IL_NpcAI() );
        _hooks.Add( new IL_NpcPreAI() );
        _hooks.Add( new IL_ItemUpdateInventory() );
        _hooks.Add( new IL_Counters() );

        foreach ( var hook in _hooks ) hook.Load( targetMod );
    }

    public override void Unload() {
        foreach ( var hook in _hooks ) hook.Unload();
        _hooks.Clear();
        System_Cache.Unload();
    }

    public override void PostSetupContent() { System_Cache.BuildCache(); }

}