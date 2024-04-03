using GTA;

namespace LSCrews.Source.Data;

public abstract class Vehicles
{
    public static readonly VehicleHash[] BeginnerVehicles =
    {
        VehicleHash.Chino,
        VehicleHash.Ingot,
        VehicleHash.Primo,
        VehicleHash.Minivan,
        VehicleHash.Cheburek
        
    };
    
    public static readonly VehicleHash[] IntermediateVehicles =
    {
        VehicleHash.BJXL,
        VehicleHash.Mesa,
        VehicleHash.Buffalo,
        VehicleHash.Kuruma,
        VehicleHash.Schafter3
    };
    
    public static readonly VehicleHash[] AdvancedVehicles =
    {
        VehicleHash.Buffalo4,
        VehicleHash.Tailgater2,
        VehicleHash.Rhinehart,
    };
    
    public static readonly VehicleHash[] ExpertVehicles =
    {
        VehicleHash.Jugular,
        VehicleHash.Toros,
        VehicleHash.Cinquemila,
        VehicleHash.VStr,
    };

    public static readonly VehicleHash[] SpecialVehicles =
    {
        VehicleHash.Kuruma2,
        VehicleHash.Insurgent2,
        VehicleHash.Menacer,
        VehicleHash.NightShark
    };
}