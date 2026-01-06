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

    private static Dictionary<string, int> _idToItemTypeMap;

    private const int UPDATE_INTERVAL = 60;
    private static int _updateTimer = 0;

    public override void Load() {
        LocalPlayerActivePets = new List<int>( 64 );
        _idToItemTypeMap = new Dictionary<string, int>();

        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod targetMod ) ) return;

        Type mainType = targetMod.Code.GetType( "TouhouPetsEx.TouhouPetsEx" );
        FieldInfo instancesField = mainType?.GetField( "GEnhanceInstances", BindingFlags.Static | BindingFlags.Public );

        if ( instancesField != null ) {
            var instancesDict = instancesField.GetValue( null ) as IDictionary;
            if ( instancesDict != null ) {
                foreach ( DictionaryEntry entry in instancesDict ) {
                    if ( entry.Key is int itemType && entry.Value != null ) {
                        Type enhanceType = entry.Value.GetType();
                        string id = enhanceType.FullName ?? enhanceType.Name;
                        if ( !_idToItemTypeMap.ContainsKey( id ) ) {
                            _idToItemTypeMap[ id ] = itemType;
                        }
                    }
                }
            }
        }

        Type enhancePlayersType = targetMod.Code.GetType( "TouhouPetsEx.Enhance.Core.EnhancePlayers" );
        if ( enhancePlayersType == null ) return;

        _fieldActiveEnhance = enhancePlayersType.GetField( "ActiveEnhance", BindingFlags.Instance | BindingFlags.Public );
        _fieldActivePassiveEnhance = enhancePlayersType.GetField( "ActivePassiveEnhance", BindingFlags.Instance | BindingFlags.Public );

        if ( targetMod.TryFind( "EnhancePlayers", out ModPlayer mp ) ) {
            _prototypeEnhancePlayer = mp;
            _reflectionReady = ( _fieldActiveEnhance != null && _fieldActivePassiveEnhance != null && _idToItemTypeMap.Count > 0 );
        }
    }

    public override void Unload() {
        LocalPlayerActivePets = null;
        _prototypeEnhancePlayer = null;
        _fieldActiveEnhance = null;
        _fieldActivePassiveEnhance = null;
        _idToItemTypeMap = null;
    }

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

        ResolveAndAdd( activeList );
        ResolveAndAdd( passiveList );
    }

    private void ResolveAndAdd( IList list ) {
        if ( list == null ) return;

        foreach ( object item in list ) {
            string idString = item.ToString();
            if ( _idToItemTypeMap.TryGetValue( idString, out int itemType ) ) LocalPlayerActivePets.Add( itemType );
        }
    }

}