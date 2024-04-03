using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace LSCrews.Source;

public enum MemberState
{
    None,
    DriveVehicle,
    Walk,
    Run,
    OutOfReach,
}

public class VehicleGroup
{
    public readonly List<Member> Members = new();
    public Vehicle Vehicle { get; set; }

    public int EnteredCount = 0;

    public Member CurrentDriver { get; set; }

    public Queue<VehicleSeat> Seats = new();

    public bool IsEscortActive = false;

    private bool _isHandbrakeForced;

    public bool IsHandbrakeForced
    {
        get => _isHandbrakeForced;
        set
        {
            _isHandbrakeForced = value;
            Vehicle.IsHandbrakeForcedOn = value;
            Vehicle.IsEngineRunning = !value;
            Logger.Log("Setting handbrake to " + value);
        }
    }

    public VehicleGroup(Vehicle vehicle)
    {
        Vehicle = vehicle;
        List<VehicleSeat> seatList = ((VehicleSeat[]) Enum.GetValues(typeof(VehicleSeat))).ToList();
        Seats.Enqueue(VehicleSeat.Driver);
        Seats.Enqueue(VehicleSeat.Passenger);
        foreach (VehicleSeat seat in seatList)
        {
            if (seat == VehicleSeat.Any || seat == VehicleSeat.None || Seats.Contains(seat)) continue;
            Seats.Enqueue(seat);
        }
    }

    public void AddToGroup(Member member)
    {
        Members.Add(member);
        member.VehicleGroup = this;
        member.AssignedVehicle = Vehicle;
    }
}

public class Member
{
    public Crew Crew { get; set; }

    public Ped Character { get; set; }

    public Member Leader { get; set; }

    public Vector3 Checkpoint = Vector3.Zero;

    public bool HasCheckpointUpdated = false;

    public List<Member> Followers = new();

    public MemberState State;

    public bool HasEnteredAssignedVehicle = false;

    public VehicleGroup VehicleGroup { get; set; }

    public Vehicle AssignedVehicle { get; set; }

    private readonly Random _random = new();

    public Member(Ped character, Crew crew)
    {
        Character = character;
        Crew = crew;
        Crew.ActiveMembers.Add(this);
        Character.IsPersistent = true;
    }

    public static bool IsPedAMember(Crew crew, Ped ped)
    {
        bool result = false;
        foreach (Member member in crew.ActiveMembers)
        {
            if (member.Character == ped)
            {
                result = true;
                Logger.Log("Ped is a member.");
                break;
            }
        }

        if (!result)
        {
            Logger.Log("Ped is not a member.");
        }

        return result;
    }

    public void EnterVehicle(Vehicle vehicle, VehicleSeat seat = VehicleSeat.Any)
    {
        // Invoking Task.EnterVehicle with VehicleSeat.Driver doesn't like that there's an existing driver?
        // Also - same relationship passengers cause Task.EnterVehicle to not proceed without the required flags, however we need ResumeIfInterrupted.
        
        // For now I'm just going to delete all the peds in the vehicle and come up with a solution in the future. TODO: (Task Scheduling Queue)
        Ped existingPed = vehicle.GetPedOnSeat(seat);
        if (existingPed != null)
        {
            existingPed.Delete();
            Logger.Log("deleting ped in seat.");
        }

        AssignedVehicle = vehicle;
        Character.Task.EnterVehicle(vehicle, seat, flag: EnterVehicleFlags.ResumeIfInterupted, speed: 3.0f);
        Character.CanBeDraggedOutOfVehicle = false;
    }

    public void LeaveCrew()
    {
        if (Character != Game.Player.Character && Character.Exists())
        {
            Logger.Log("RUNNING MARK.");
            Character.MarkAsNoLongerNeeded();
            Character.AttachedBlip.Delete();
        }

        Logger.Log("Removing from activemmebers.");
        Crew.ActiveMembers.Remove(this);

        if (VehicleGroup != null)
        {
            Logger.Log("Removing from Vehicle Group");
            VehicleGroup.Members.Remove(this);

            if (Character.IsInVehicle())
            {
                VehicleGroup.EnteredCount--;
            }
        }
    }

    public void LeaveVehicleGroup()
    {
        VehicleGroup.Members.Remove(this);
        VehicleGroup = null;
    }

    public void AssignNewLeader(int maxFollowers)
    {
        Member previousLeader = Leader;
        Leader = Crew.ActiveMembers.Find(leader =>
            leader.Followers.Count < maxFollowers && !leader.Character.IsDead && leader != this && leader != previousLeader);
        Leader.Followers.Add(this);

        Logger.Log("Assigning leader to " + Leader.Character.Handle);
    }

    public void AssignAttributes()
    {
        AssignNewLeader(2);
        Character.RelationshipGroup = Game.Player.Character.RelationshipGroup;
        Character.NeverLeavesGroup = true;

        // Weapons
        WeaponHash[] weapons = Level.GetWeapons(Crew.CrewLevel);
        WeaponHash weaponHash = weapons[_random.Next(0, weapons.Length)];
        Character.Weapons.Give(weaponHash, 9999, true, true);
        Logger.Log("Setting Weapon to " + weaponHash);

        // Health
        int health = Level.GetHealth(Crew.CrewLevel);
        Character.MaxHealth = health;
        Character.Health = health;
        Logger.Log("Setting Health to " + health);

        // Movement Speed
        float movementSpeed = Level.GetMovementSpeed(Crew.CrewLevel);
        Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, Character, movementSpeed);
        Logger.Log("Setting Movement Speed to " + movementSpeed);

        // Blip
        Blip blip = Character.AddBlip();
        blip.ShowsHeadingIndicator = true;
        blip.Scale = 0.8f;
        blip.Color = BlipColor.Blue;
        blip.DisplayType = BlipDisplayType.BothMapSelectable;
        blip.RotationFloat = Character.Heading;
        Function.Call((Hash) 0x2B6D467DAB714E8D, true);

        // Combat Attributes & Config Flags
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 0, true); // CanUseCover
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 1, true); // CanUseVehicles
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 2, true); // CanDoDrivebys
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 3, true); // CanLeaveVehicle
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 4, true); // CanUseDynamicStrafeDecisions
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 12, true); // BlindFireWhenInCover
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 13, false); // Aggressive
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 14, true); // CanInvestigate
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 20, true); // CanTauntInVehicle
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 21, true); // CanChaseTargetOnFoot
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 22, true); // WillDragInjuredPedsToSafety
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 27, true); // PerfectAccuracy
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 28, true); // CanUseFrustratedAdvance
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 34, true); // CanUsePeekingVariations
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 38, true); // DisableBulletReactions
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 42, true); // CanFlank
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 43, true); // SwitchToAdvanceIfCantFindCover
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 46, true); // CanFightArmedPedsWhenNotArmed
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 55, true); // CanSeeUnderwaterPeds
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 58, true); // DisableFleeFromCombat
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 60, true); // CanThrowSmokeGrenade
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 63, false); // FleesFromInvincibleOpponents
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 71, false); // PermitChargeBeyondDefensiveArea
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Character.Handle, 86, true); // AllowDogFighting
        Character.SetConfigFlag(42, true); // DontInfluenceWantedLevel
        Character.SetConfigFlag(128, true); // CanBeAgitated
        Character.SetConfigFlag(39, true); // DisableEvasiveDives
        Character.SetConfigFlag(294, true); // DisableShockingEvents
        Character.SetConfigFlag(301, true); // DisablePedConstraints
        Character.SetConfigFlag(229, true); // DisablePanicInVehicle
        Character.SetConfigFlag(188, true); // DisableHurt
    }

    public void DisplayHandles()
    {
        float memberDist = 0.0f;
        if (Leader != null)
        {
            memberDist = Vector3.Distance2D(Leader.Character.Position, Character.Position);
        }

        string text = $"Handle {Character.Handle}\n" +
                      $"Leader {Leader?.Character.Handle}\n" +
                      $"Distance {memberDist}\n" +
                      $"IsInCombat {Character.IsInCombat}\n" +
                      $"IsDriver {VehicleGroup?.CurrentDriver == this}\n" +
                      $"TaskStatus: {Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, Character, 0x950B6492)}";

        Function.Call(Hash.SET_TEXT_OUTLINE);
        Function.Call(Hash.SET_TEXT_JUSTIFICATION, (int) Alignment.Center);
        Function.Call(Hash.SET_TEXT_SCALE, 1.0f, 0.2f);
        Function.Call(Hash.SET_DRAW_ORIGIN, Character.Position.X,
            Character.Position.Y,
            Character.Position.Z, 0);
        Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
        Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, 0, 0);
        Function.Call(Hash.CLEAR_DRAW_ORIGIN);
    }
}