using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public static class System_Cache {

    public static List<Tuple<object, MethodInfo>> TileDrawEffects = new();
    public static List<Tuple<object, MethodInfo>> NpcAI = new();
    public static List<Tuple<object, MethodInfo>> NpcPreAI = new();
    public static List<Tuple<object, MethodInfo>> ItemUpdateInventory = new();

    public static void Unload() { TileDrawEffects.Clear(); NpcAI.Clear(); NpcPreAI.Clear(); ItemUpdateInventory.Clear(); }

    public static void BuildCache() {
        Unload();

        if ( !ModLoader.TryGetMod( "TouhouPetsEx", out Mod 模组 ) ) return;

        Type 类型_TouhouPetsEx = 模组.Code.GetType( "TouhouPetsEx.TouhouPetsEx" );
        Type 类型_BaseEnhance = 模组.Code.GetType( "TouhouPetsEx.Enhance.Core.BaseEnhance" );

        FieldInfo 字段_GEnhanceInstances = 类型_TouhouPetsEx?.GetField( "GEnhanceInstances", BindingFlags.Static | BindingFlags.Public );
        if ( 字段_GEnhanceInstances == null ) return;

        object 对象_GEnhanceInstances = 字段_GEnhanceInstances.GetValue( null );
        if ( 对象_GEnhanceInstances == null ) return;

        PropertyInfo 属性_Values = 对象_GEnhanceInstances.GetType().GetProperty( "Values" );
        if ( 属性_Values == null ) return;

        var 宠物集合 = 属性_Values.GetValue( 对象_GEnhanceInstances ) as IEnumerable;
        if ( 宠物集合 == null ) return;

        foreach ( object 宠物 in 宠物集合 ) {
            if ( 宠物 == null ) continue;
            Type 宠物类型 = 宠物.GetType();

            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "TileDrawEffects", TileDrawEffects );
            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "NPCAI", NpcAI );
            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "NPCPreAI", NpcPreAI );
            注册缓存( 宠物, 宠物类型, 类型_BaseEnhance, "ItemUpdateInventory", ItemUpdateInventory );
        }
    }

    private static void 注册缓存( object 实例, Type 实例类型, Type 基类类型, string 方法名, List<Tuple<object, MethodInfo>> 缓存列表 ) {
        MethodInfo 方法 = 实例类型.GetMethod( 方法名, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        if ( 方法 != null && 方法.DeclaringType != 基类类型 ) 缓存列表.Add( new Tuple<object, MethodInfo>( 实例, 方法 ) );
    }

}