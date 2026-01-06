using System.Collections.Generic;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Configs;
using TouhouPetsExOptimization.Hooking;
using TouhouPetsExOptimization.Hooking.IL;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization;



public class TouhouPetsExOptimization : Mod {

    private List<BaseHook> _hooks = new();

    public override void Load() {
        MainConfigCache.Update();

        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) return;

        _hooks.Add( new IL_GEnhanceTile_DrawEffects() );
        _hooks.Add( new IL_GEnhanceNPCs_PreAI() );
        _hooks.Add( new IL_GEnhanceNPCs_AI() );
        _hooks.Add( new IL_GEnhanceItems_PostDrawInInventory() );
        _hooks.Add( new IL_GEnhanceItems_UpdateInventory() );
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