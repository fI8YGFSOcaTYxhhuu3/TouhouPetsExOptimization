using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TouhouPetsExOptimization.Configs;
using TouhouPetsExOptimization.Systems;

namespace TouhouPetsExOptimization.LegacySimulation;



public class Legacy_GlobalTile : GlobalTile {
    public override void DrawEffects( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData ) {
        if ( MainConfigCache.优化模式_GEnhanceTile_DrawEffects != MainConfigs.优化模式.旧版模拟 ) return;
        TileDrawInfo drawDataCopy = drawData;
        ProcessDemonismAction( ( enhance ) => { enhance.TileDrawEffects( i, j, type, spriteBatch, ref drawDataCopy ); } );
        drawData = drawDataCopy;
    }
    public static void ProcessDemonismAction( Action<Legacy_BaseEnhance> action ) {
        foreach ( Legacy_BaseEnhance enhance in Legacy_System.EnhanceDict.Values ) {
            if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_TileDrawEffects++;
            action( enhance );
        }
    }
}

public class Legacy_GlobalNPC : GlobalNPC {
    public override void AI( NPC npc ) {
        if ( MainConfigCache.优化模式_GEnhanceNPCs_AI != MainConfigs.优化模式.旧版模拟 ) return;
        ProcessDemonismAction_AI( ( enhance ) => { enhance.NPCAI( npc ); } );
    }
    public override bool PreAI( NPC npc ) {
        if ( MainConfigCache.优化模式_GEnhanceNPCs_PreAI != MainConfigs.优化模式.旧版模拟 ) return true;
        bool? reesult = ProcessDemonismAction_PreAI( false, ( enhance ) => enhance.NPCPreAI( npc ) );
        return reesult ?? base.PreAI( npc );
    }
    public static void ProcessDemonismAction_AI( Action<Legacy_BaseEnhance> action ) {
        foreach ( Legacy_BaseEnhance enhance in Legacy_System.EnhanceDict.Values ) {
            if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_NPCAI++;
            action( enhance );
        }
    }
    public static bool? ProcessDemonismAction_PreAI( bool? booleanValue, Func<Legacy_BaseEnhance, bool?> action ) {
        if ( booleanValue == null ) {
            bool? ret = null;
            foreach ( Legacy_BaseEnhance enhance in Legacy_System.EnhanceDict.Values ) {
                if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_NPCPreAI++;
                bool? a = action( enhance );
                if ( a != null ) ret = a;
            }
            return ret;
        }
        else {
            bool? ret = null;
            foreach ( Legacy_BaseEnhance enhance in Legacy_System.EnhanceDict.Values ) {
                if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_NPCPreAI++;
                bool? a = action( enhance );
                if ( a == booleanValue ) return a;
                else if ( a != null ) ret = a;
            }
            return ret;
        }
    }
}

public class Legacy_GlobalItem : GlobalItem {
    public override void PostDrawInInventory( Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale ) {
        if ( MainConfigCache.优化模式_GEnhanceItems_PostDrawInInventory != MainConfigs.优化模式.旧版模拟 ) return;
        ProcessDemonismAction( ( enhance ) => enhance.ItemPostDrawInInventory( item, spriteBatch, position, frame, drawColor, itemColor, origin, scale ) );
    }
    public override void UpdateInventory( Item item, Player player ) {
        if ( MainConfigCache.优化模式_GEnhanceItems_UpdateInventory != MainConfigs.优化模式.旧版模拟 ) return;
        ProcessDemonismAction( ( enhance ) => { enhance.ItemUpdateInventory( item, player ); } );
    }
    public static void ProcessDemonismAction( Action<Legacy_BaseEnhance> action ) {
        foreach ( Legacy_BaseEnhance enhance in Legacy_System.EnhanceDict.Values ) {
            if ( MainConfigCache.性能监控 ) System_Counter.调用计数_BaseEnhance_PostDrawInInventory_UpdateInventory++;
            action( enhance );
        }
    }
}