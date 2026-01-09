using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_补丁自检 : ModSystem {

    public static bool 缓存状态_动态数据 = false;
    public static bool 缓存状态_静态数据 = false;

    public static bool 未发生已知错误 { get { return 缓存状态_动态数据 && 缓存状态_静态数据; } }

    public override void Unload() {
        缓存状态_动态数据 = false;
        缓存状态_静态数据 = false;
    }

}