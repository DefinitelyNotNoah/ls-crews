using System;
using GTA;
using LSCrews.Source.Data;

namespace LSCrews.Source;

public abstract class Level
{
    public static bool CanLevelUp(int level, int experience)
    {
        bool result = experience >= ProjectedExperience(level);
        if (result)
            Logger.Log("Can level up. XP = " + experience);
        return result;
    }

    public static int ProjectedExperience(int level) => (int) Math.Floor(3 * Math.Pow(level, 2));

    public static WeaponHash[] GetWeapons(int level)
    {
        return level switch
        {
            < 5 => Weapons.BeginnerWeapons,
            < 10 => Weapons.IntermediateWeapons,
            < 15 => Weapons.AdvancedWeapons,
            < 30 => Weapons.ExpertWeapons,
            _ => Weapons.SpecialWeapons
        };
    }


    public static VehicleHash[] GetVehicles(int level)
    {
        return level switch
        {
            < 5 => Vehicles.BeginnerVehicles,
            < 10 => Vehicles.IntermediateVehicles,
            < 15 => Vehicles.AdvancedVehicles,
            < 30 => Vehicles.ExpertVehicles,
            _ => Vehicles.SpecialVehicles,
        };
    }

    public static int GetHealth(int level)
    {
        return level switch
        {
            < 5 => 200,
            < 10 => 250,
            < 15 => 300,
            < 30 => 350,
            _ => 400,
        };
    }

    public static float GetMovementSpeed(int level)
    {
        return level switch
        {
            < 5 => 1.0f,
            < 10 => 1.05f,
            < 15 => 1.1f,
            < 30 => 1.15f,
            _ => 1.2f,
        };
    }

    public static int GetPedCount(int level)
    {
        return level switch
        {
            < 5 => 4,
            < 10 => 6,
            < 15 => 8,
            < 20 => 10,
            < 25 => 12,
            < 30 => 14,
            < 35 => 16,
            < 40 => 18,
            _ => 20
        };
    }
}