using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_State : ModSystem {

    public static List<int> LocalPlayerActivePets = new List<int>();

    private static ModPlayer _prototypeEnhancePlayer;
    private static FieldInfo _fieldActiveEnhance;
    private static FieldInfo _fieldActivePassiveEnhance;
    private static bool _reflectionReady = false;

    private const int UPDATE_INTERVAL = 30; 
    private static int _updateTimer = 0;

    public override void Load() {
        LocalPlayerActivePets = new List<int>( 128 );

        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) return;

        Type enhancePlayersType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhancePlayers" );
        if ( enhancePlayersType == null ) return;

        _fieldActiveEnhance = enhancePlayersType.GetField( "ActiveEnhance", BindingFlags.Instance | BindingFlags.Public );
        _fieldActivePassiveEnhance = enhancePlayersType.GetField( "ActivePassiveEnhance", BindingFlags.Instance | BindingFlags.Public );

        if ( targetMod.TryFind( "EnhancePlayers", out ModPlayer mp ) ) {
            _prototypeEnhancePlayer = mp;
            _reflectionReady = ( _fieldActiveEnhance != null && _fieldActivePassiveEnhance != null );
        }
    }

    public override void Unload() { LocalPlayerActivePets = null; _prototypeEnhancePlayer = null; _fieldActiveEnhance = null; _fieldActivePassiveEnhance = null; }

    public override void PostUpdateEverything() {
        if ( Main.gameMenu ) {
            LocalPlayerActivePets.Clear();
            _updateTimer = UPDATE_INTERVAL;
            return;
        }

        _updateTimer++;
        if ( _updateTimer < UPDATE_INTERVAL ) return; 
        _updateTimer = 0;

        LocalPlayerActivePets.Clear();

        if ( !_reflectionReady || Main.LocalPlayer == null || !Main.LocalPlayer.active ) return;

        ModPlayer enhancePlayerInstance = Main.LocalPlayer.GetModPlayer( _prototypeEnhancePlayer );
        if ( enhancePlayerInstance == null ) return;

        var activeList = _fieldActiveEnhance.GetValue( enhancePlayerInstance ) as IList;
        var passiveList = _fieldActivePassiveEnhance.GetValue( enhancePlayerInstance ) as IList;

        if ( activeList != null ) foreach ( int id in activeList ) LocalPlayerActivePets.Add( id );
        if ( passiveList != null ) foreach ( int id in passiveList ) LocalPlayerActivePets.Add( id );
    }

}