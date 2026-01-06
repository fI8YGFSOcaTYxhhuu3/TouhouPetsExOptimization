using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Configs;



public static class MainConfigCache {

    public static bool 性能监控;
    public static MainConfigs.优化模式 优化模式_GEnhanceTile_DrawEffects;
    public static MainConfigs.优化模式 优化模式_GEnhanceItems_UpdateInventory;
    public static MainConfigs.优化模式 优化模式_GEnhanceNPCs_PreAI_AI;

    public static void Update() {
        var config = ModContent.GetInstance<MainConfigs>();
        if ( config == null ) return;

        性能监控 = config.性能监控;
        优化模式_GEnhanceTile_DrawEffects = config.优化模式_GEnhanceTile_DrawEffects;
        优化模式_GEnhanceItems_UpdateInventory = config.优化模式_GEnhanceItems_UpdateInventory;
        优化模式_GEnhanceNPCs_PreAI_AI = config.优化模式_GEnhanceNPCs_PreAI_AI;
    }

}