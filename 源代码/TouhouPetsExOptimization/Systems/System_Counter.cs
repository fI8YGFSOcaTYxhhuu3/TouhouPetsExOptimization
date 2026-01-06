using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using TouhouPetsExOptimization.Configs;

namespace TouhouPetsExOptimization.Systems;



public class System_Counter : ModSystem {

    public static long 调用计数_GEnhanceTile_DrawEffects = 0;
    public static long 调用计数_GEnhanceNPCs_AI = 0;
    public static long 调用计数_GEnhanceNPCs_PreAI = 0;
    public static long 调用计数_GEnhanceItems_UpdateInventory = 0;
    public static long 调用计数_BaseEnhance_TileDrawEffects = 0;
    public static long 调用计数_BaseEnhance_PreAI_AI = 0;
    public static long 调用计数_BaseEnhance_UpdateInventory = 0;

    private static long 显示数值_帧率 = 0;

    private static long 显示数值_调用计数_GEnhanceTile_DrawEffects = 0;
    private static long 显示数值_调用计数_GEnhanceNPCs_AI = 0;
    private static long 显示数值_调用计数_GEnhanceNPCs_PreAI = 0;
    private static long 显示数值_调用计数_GEnhanceItems_UpdateInventory = 0;

    private static long 显示数值_调用计数_BaseEnhance_TileDrawEffects = 0;
    private static long 显示数值_调用计数_BaseEnhance_PreAI_AI = 0;
    private static long 显示数值_调用计数_BaseEnhance_UpdateInventory = 0;

    private double 计时器 = 0;

    public override void Unload() {
        调用计数_GEnhanceTile_DrawEffects = 0;
        调用计数_GEnhanceNPCs_AI = 0;
        调用计数_GEnhanceNPCs_PreAI = 0;
        调用计数_GEnhanceItems_UpdateInventory = 0;
        调用计数_BaseEnhance_TileDrawEffects = 0;
        调用计数_BaseEnhance_PreAI_AI = 0;
        调用计数_BaseEnhance_UpdateInventory = 0;
    }

    public override void UpdateUI( GameTime gameTime ) {
        计时器 += gameTime.ElapsedGameTime.TotalSeconds;
        if ( 计时器 >= 1.0 ) {
            显示数值_帧率 = Main.frameRate;

            显示数值_调用计数_GEnhanceTile_DrawEffects = 调用计数_GEnhanceTile_DrawEffects;
            显示数值_调用计数_BaseEnhance_TileDrawEffects = 调用计数_BaseEnhance_TileDrawEffects;

            显示数值_调用计数_GEnhanceNPCs_AI = 调用计数_GEnhanceNPCs_AI;
            显示数值_调用计数_GEnhanceNPCs_PreAI = 调用计数_GEnhanceNPCs_PreAI;
            显示数值_调用计数_BaseEnhance_PreAI_AI = 调用计数_BaseEnhance_PreAI_AI;

            显示数值_调用计数_GEnhanceItems_UpdateInventory = 调用计数_GEnhanceItems_UpdateInventory;
            显示数值_调用计数_BaseEnhance_UpdateInventory = 调用计数_BaseEnhance_UpdateInventory;

            调用计数_GEnhanceTile_DrawEffects = 0;
            调用计数_BaseEnhance_TileDrawEffects = 0;
            调用计数_GEnhanceNPCs_AI = 0;
            调用计数_GEnhanceNPCs_PreAI = 0;
            调用计数_BaseEnhance_PreAI_AI = 0;
            调用计数_GEnhanceItems_UpdateInventory = 0;
            调用计数_BaseEnhance_UpdateInventory = 0;

            计时器 = 0;
        }
    }

    public override void ModifyInterfaceLayers( List<GameInterfaceLayer> layers ) {
        var 模组配置 = ModContent.GetInstance<MainConfigs>();
        if ( 模组配置 == null || !模组配置.性能监控 ) return;

        int 原版界面索引 = layers.FindIndex( layer => layer.Name.Equals( "Vanilla: Resource Bars" ) );
        if ( 原版界面索引 == -1 ) return;

        layers.Insert( 原版界面索引, new LegacyGameInterfaceLayer(
            "TouhouPetsExOptimization: 调试信息",
            delegate {
                string 文本 = $"[性能监控]\n" +
                                $"当前帧率 = {显示数值_帧率:N0}\n" +
                                $"\n" +
                                $"优化模式 - GEnhanceTile.DrawEffects: {模组配置.优化模式_GEnhanceTile_DrawEffects.ToString()}\n" +
                                $"优化模式 - GEnhanceNPCs.PreAI & AI: {模组配置.优化模式_GEnhanceNPCs_PreAI_AI.ToString()}\n" +
                                $"优化模式 - GEnhanceItems.UpdateInventory: {模组配置.优化模式_GEnhanceItems_UpdateInventory.ToString()}\n" +
                                $"\n" +
                                $"每秒调用次数 - GEnhanceTile.DrawEffects = {显示数值_调用计数_GEnhanceTile_DrawEffects:N0}\n" +
                                $"每秒调用次数 - BaseEnhance.TileDrawEffects = {显示数值_调用计数_BaseEnhance_TileDrawEffects:N0}\n" +
                                $"\n" +
                                $"每秒调用次数 - GEnhanceNPCs.PreAI = {显示数值_调用计数_GEnhanceNPCs_PreAI:N0}\n" +
                                $"每秒调用次数 - GEnhanceNPCs.AI = {显示数值_调用计数_GEnhanceNPCs_AI:N0}\n" +
                                $"每秒调用次数 - BaseEnhance.PreAI & AI = {显示数值_调用计数_BaseEnhance_PreAI_AI:N0}\n" +
                                $"\n" +
                                $"每秒调用次数 - GEnhanceItems.UpdateInventory = {显示数值_调用计数_GEnhanceItems_UpdateInventory:N0}\n" +
                                $"每秒调用次数 - BaseEnhance.UpdateInventory = {显示数值_调用计数_BaseEnhance_UpdateInventory:N0}\n";
                Utils.DrawBorderString( Main.spriteBatch, 文本, new Vector2( 10, 150 ), Color.Lime, 0.8f );
                return true;
            },
            InterfaceScaleType.UI )
        );
    }

}