using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace TouhouPetsExOptimization;



public class Disclaimer : ModPlayer {

    private bool _hasShownDisclaimer = false;

    public override void OnEnterWorld() { _hasShownDisclaimer = false; }

    public override void PostUpdate() {
        if ( Main.netMode == NetmodeID.Server || _hasShownDisclaimer ) return;
        if ( Player.whoAmI != Main.myPlayer ) return;
        if ( !ModLoader.HasMod( "TouhouPetsEx" ) ) { _hasShownDisclaimer = true; return; }

        string message =
            $"[东方小祖宗 - 性能缺陷修复补丁说明]\n" +
            $"0. 本补丁目前已适配《东方小祖宗》 v1.1.5.818 版本。\n" +
            $"1. 本补丁采用大量“防御性编程”设计，可应对大多数函数内容变更、函数签名变更、模组缺失、类缺失、函数缺失、字段缺失等情况。\n" +
            $"2. 若前置模组更新导致代码不兼容，本补丁预计将自动失效并回退至原版逻辑，极难导致游戏崩溃。\n" +
            $"3. 如果前置模组或本补丁出现任何问题，请随意向本补丁模组反馈。\n" +
            $"4. 本补丁的代码公开于 GitHub，请随意检查。\n" +
            $"GitHub 地址：https://github.com/fI8YGFSOcaTYxhhuu3/TouhouPetsExOptimization";

        Main.NewText( message, Color.Lime );

        _hasShownDisclaimer = true;
    }

}