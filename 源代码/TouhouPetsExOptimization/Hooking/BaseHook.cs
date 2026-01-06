using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Hooking;



public abstract class BaseHook {

    public abstract void Load( Mod targetMod );
    public abstract void Unload();

}