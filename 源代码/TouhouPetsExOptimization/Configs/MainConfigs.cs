using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TouhouPetsExOptimization.Configs;



public class MainConfigs : ModConfig {

    public enum 优化模式 {
        关闭补丁,
        智能缓存,
        暴力截断
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header( "优化模式" )]

    [DefaultValue( 优化模式.智能缓存 )]
    public 优化模式 优化模式_GEnhanceTile_DrawEffects;

    [DefaultValue( 优化模式.智能缓存 )]
    public 优化模式 优化模式_GEnhanceNPCs_PreAI_AI;

    [DefaultValue( 优化模式.智能缓存 )]
    public 优化模式 优化模式_GEnhanceItems_UpdateInventory;

    [Header( "调试选项" )]

    [DefaultValue( false )]
    public bool 性能监控;

    public override void OnChanged() { MainConfigCache.Update(); }

}