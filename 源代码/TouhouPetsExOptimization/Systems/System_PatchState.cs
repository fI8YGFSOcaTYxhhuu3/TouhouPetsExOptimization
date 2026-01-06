using Terraria.ModLoader;

namespace TouhouPetsExOptimization.Systems;



public class System_PatchState : ModSystem {

    public static bool IsActivePetListReaderWorking = false;
    public static bool IsIdMappingWorking = false;
    public static bool IsCacheBuilt = false;

    public static bool IsSafeToOptimize { get { return IsActivePetListReaderWorking && IsIdMappingWorking && IsCacheBuilt; } }

    public override void Unload() {
        IsActivePetListReaderWorking = false;
        IsIdMappingWorking = false;
        IsCacheBuilt = false;
    }

    public static string GetStatusReport() {
        return $"PetReader: {IsActivePetListReaderWorking}, IdMap: {IsIdMappingWorking}, Cache: {IsCacheBuilt} => Safe: {IsSafeToOptimize}";
    }

}