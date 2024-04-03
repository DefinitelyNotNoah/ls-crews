using GTA;

namespace LSCrews.Source.Data;

public abstract class Weapons
{
    public static readonly WeaponHash[] BeginnerWeapons =
    {
        WeaponHash.Pistol,
        WeaponHash.SNSPistol,
        WeaponHash.SawnOffShotgun,
        WeaponHash.MiniSMG,
        WeaponHash.MicroSMG,
        WeaponHash.MachinePistol,
        WeaponHash.DoubleBarrelShotgun
    };

    public static readonly WeaponHash[] IntermediateWeapons =
    {
        WeaponHash.SMG,
        WeaponHash.CombatPDW,
        WeaponHash.AssaultSMG,
        WeaponHash.APPistol,
        WeaponHash.Pistol50,
        WeaponHash.CombatPistol,
        WeaponHash.HeavyPistol,
        WeaponHash.PumpShotgun,
        WeaponHash.BullpupRifle,
        WeaponHash.SweeperShotgun,
        WeaponHash.CompactRifle
    };

    public static readonly WeaponHash[] AdvancedWeapons =
    {
        WeaponHash.AssaultRifle,
        WeaponHash.CarbineRifle,
        WeaponHash.AssaultShotgun,
        WeaponHash.HeavyShotgun,
        WeaponHash.CombatShotgun,
        WeaponHash.Revolver,
        WeaponHash.SniperRifle
    };

    public static readonly WeaponHash[] ExpertWeapons =
    {
        WeaponHash.MG,
        WeaponHash.AdvancedRifle,
        WeaponHash.BullpupRifle,
        WeaponHash.SpecialCarbine,
        WeaponHash.MilitaryRifle,
        WeaponHash.HeavyRifle,
        WeaponHash.SpecialCarbine,
        WeaponHash.HeavySniper,
        WeaponHash.MarksmanRifle,
        WeaponHash.PrecisionRifle
    };

    public static readonly WeaponHash[] SpecialWeapons =
    {
        WeaponHash.Minigun,
        WeaponHash.GrenadeLauncher,
        WeaponHash.HomingLauncher,
        WeaponHash.RPG,
        WeaponHash.Railgun,
        WeaponHash.CompactGrenadeLauncher,
        WeaponHash.Widowmaker,
    };
}