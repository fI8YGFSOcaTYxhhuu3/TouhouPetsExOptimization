using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace TouhouPetsExOptimization.LegacySimulation;



public abstract class Legacy_BaseEnhance {
    public virtual void ItemUpdateInventory( Item item, Player player ) { }
    public virtual bool? NPCPreAI( NPC npc ) { return null; }
    public virtual void NPCAI( NPC npc ) { }
    public virtual void TileDrawEffects( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData ) { }
}

public class Legacy_ChildEnhance : Legacy_BaseEnhance {
    public override void ItemUpdateInventory( Item item, Player player ) { if ( item.type == 0 ) return; }
    public override bool? NPCPreAI( NPC npc ) { return null; }
    public override void NPCAI( NPC npc ) { if ( npc.whoAmI == -1 ) return; }
    public override void TileDrawEffects( int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData ) { if ( type == 0 ) return; }
}

public static class Legacy_System {

    private const int EnhanceCount = 59;
    public static Dictionary<int, Legacy_BaseEnhance> EnhanceDict;

    static Legacy_System() {
        EnhanceDict = new Dictionary<int, Legacy_BaseEnhance>();
        for ( int i = 0; i < EnhanceCount; i++ ) EnhanceDict.Add( i, new Legacy_ChildEnhance() );
    }

}