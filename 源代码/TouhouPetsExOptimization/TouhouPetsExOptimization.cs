using log4net;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Configs;
using TouhouPetsExOptimization.Hooking;
using TouhouPetsExOptimization.Hooking.IL;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization;



public class TouhouPetsExOptimization : Mod {

    private static readonly ILog 日志 = ModContent.GetInstance<TouhouPetsExOptimization>().Logger;
    private static void 记录( string 文本 ) => 日志.Warn( $"[TouhouPetsExOptimization] {文本}" );

    private static Mod 模组_东方小祖宗_缓存;
    public static Mod 模组_东方小祖宗 {
        get {
            if ( 模组_东方小祖宗_缓存 != null ) return 模组_东方小祖宗_缓存;
            模组_东方小祖宗_缓存 = ModLoader.Mods.FirstOrDefault( m => m.Name.Equals( "TouhouPetsEx" ) );
            return 模组_东方小祖宗_缓存;
        }
    }

    private List<BaseHook> _hooks = new();

    public override void Unload() {
        模组_东方小祖宗_缓存 = null;
        foreach ( var hook in _hooks ) hook.Unload();
        _hooks.Clear();
    }

    public override void PostSetupContent() {
        MainConfigCache.Update();

        if ( 模组_东方小祖宗 == null ) { 记录( "未找到前置模组 TouhouPetsEx" ); return; }

        _hooks.Add( new IL_GEnhanceTile_DrawEffects() );
        _hooks.Add( new IL_GEnhanceNPCs_PreAI() );
        _hooks.Add( new IL_GEnhanceNPCs_AI() );
        _hooks.Add( new IL_GEnhanceItems_PostDrawInInventory() );
        _hooks.Add( new IL_GEnhanceItems_UpdateInventory() );
        _hooks.Add( new IL_Counters() );

        foreach ( var hook in _hooks ) hook.Load( 模组_东方小祖宗 );

        System_缓存_静态数据.构筑();
        System_缓存_动态数据.构筑();
    }

}