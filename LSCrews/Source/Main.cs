using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LSCrews.Source.Data;
using LSCrews.Source.Menu;
using LSCrews.Source.Menu.ModelData;
using LSCrews.Source.Menu.PlaceholderData;
using Control = GTA.Control;
using Font = GTA.UI.Font;
using Screen = GTA.UI.Screen;

namespace LSCrews.Source;

public class Main : Script
{
    private bool IsEntityInsideRadius(Entity entity, Vector3 position, float distance) =>
        Vector3.Distance2D(entity.Position, position) * 2 < distance;

    private const float MarkerXy = 2.0f;
    private const float SearchRange = 75.0f;
    private readonly Vector3 _scale = new(MarkerXy, MarkerXy, 0.5f);
    private readonly Color _color = Color.FromArgb(255, 243, 225, 107);
    private readonly Random _random = new();
    public static int WantedLevelBeforeEvasion;

    private CrewMenu CrewMenu { get; set; }

    public Main()
    {
        // Establish Directories
        StorageManager.EstablishAllDirectories();

        // Used for development.
        Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Game.Player.Character, true, false);

        Tick += OnTick;
        KeyDown += OnKeyDown;

        // Search through existing crew files and add to crews list.
        StorageManager.UpdateCrews(StorageManager.CrewDirectory);
        CrewMenu = new CrewMenu();
        CrewMenu.CurrentCreateMenu.UpdateListOfCrews();

        // In the event that the client refreshes the script. Let's check the positions
        // for all peds & blips known to the client. If any blip or ped is in the same positions as crew, delete.
        foreach (Crew crew in Crew.CrewList)
        {
            UpdateCrewBlips(crew);

            foreach (var placeholder in crew.PlaceholderModels)
            {
                foreach (var ped in World.GetAllPeds())
                {
                    if (ped.IsPlayer)
                    {
                        Logger.Log("PED IS PLAYER SKIP");
                    }
                    else
                    {
                        Vector3 pedPos = new(ped.Position.X, ped.Position.Y, ped.Position.Z - 1);
                        if (pedPos == placeholder.position)
                        {
                            ped.Delete();
                            Logger.Log("Deleting Ped.");
                        }
                    }
                }
            }

            foreach (Blip blip in World.GetAllBlips())
            {
                if (blip.Handle == crew.Blip.Handle) continue;

                if (blip.Position == crew.Blip.Position)
                {
                    blip.Delete();
                }
            }
        }
    }

    private void UpdateCrewBlips(Crew crew)
    {
        // Add the blips only once. (Called here in game loop to check for newly created crews.)
        if (crew.Blip == null)
        {
            Vector3 pos = crew.MarkerPosition;
            crew.Blip = World.CreateBlip(pos);
            crew.Blip.Sprite = BlipSprite.Friend;
            crew.Blip.Color = BlipColor.Yellow;
            crew.Blip.Name = crew.CrewName;
            crew.Blip.IsShortRange = true;
        }
    }

    public void DrawTitle(Vector3 position, string text)
    {
        Function.Call(Hash.SET_TEXT_OUTLINE);
        Function.Call(Hash.SET_TEXT_JUSTIFICATION, (int) Alignment.Center);
        Function.Call(Hash.SET_TEXT_SCALE, 1.0f, 0.4f);
        Function.Call(Hash.SET_TEXT_FONT, Font.Pricedown);
        Function.Call(Hash.SET_DRAW_ORIGIN, position.X, position.Y, position.Z, 0);
        Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
        Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, 0, 0);
        Function.Call(Hash.CLEAR_DRAW_ORIGIN);
    }

    private void OnKeyDown(object sender, EventArgs e)
    {
        string key = ((KeyEventArgs) e).KeyCode.ToString();
        if (key == "B" && !CrewMenu.CurrentCreateMenu.Pool.AreAnyVisible)
        {
            CrewMenu.CurrentCreateMenu.LandingMenu.Visible = !CrewMenu.CurrentCreateMenu.LandingMenu.Visible;
        }
    }

    private void OnTick(object sender, EventArgs args)
    {
        CrewMenu.CurrentCreateMenu.Pool.Process();

        if (CrewMenu.CurrentCreateMenu.Pool.AreAnyVisible)
        {
            World.DrawMarker(MarkerType.VerticalCylinder, CrewMenu.TemporaryMarkerPos, Vector3.Zero, Vector3.Zero,
                _scale,
                _color);
        }

        // Handle Wanted Level
        if (Game.Player.WantedLevel > 0 && WantedLevelBeforeEvasion != Game.Player.WantedLevel)
        {
            WantedLevelBeforeEvasion = Game.Player.WantedLevel;
            Logger.Log("Updating Wanted Level.");
        }

        foreach (Crew crew in Crew.CrewList.ToList())
        {
            UpdateCrewBlips(crew);

            // Handle the crew hiring process below and member instance.
            if (IsEntityInsideRadius(Game.Player.Character, crew.MarkerPosition, 200))
            {
                World.DrawMarker(MarkerType.VerticalCylinder, crew.MarkerPosition, Vector3.Zero, Vector3.Zero,
                    _scale, _color);

                // Here we'll loop through all of the placeholder peds to see if they've been deleted while in range.
                // If so we can just regenerate them.
                int pedCountCheck = 0;
                foreach (Ped ped in crew.Placeholders.ToList())
                {
                    if (!ped.Exists())
                    {
                        pedCountCheck++;
                    }

                    if (pedCountCheck == crew.Placeholders.Count)
                    {
                        crew.HavePlaceholdersSpawned = false;
                        crew.Placeholders.Remove(ped);
                    }
                }

                // There's a logic error here preventing DrawTitle() from displaying the level if the placeholders get deleted while the player is in range.
                if (!crew.HavePlaceholdersSpawned)
                {
                    foreach (var ped in crew.PlaceholderModels)
                    {
                        Ped placeholderPed = World.CreatePed((PedHash) ped.hash, ped.position, ped.heading);
                        PlaceholderMenu.SetPlaceholderAttributes(placeholderPed);
                        crew.Placeholders.Add(placeholderPed);
                    }

                    crew.HavePlaceholdersSpawned = true;
                }

                // TODO: Line of sight to crew.Placeholders[0] is pretty inefficient and will likely need to be changed later on.
                bool lineOfSight = Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, Game.Player.Character, crew.Placeholders[0], 17);
                if (IsEntityInsideRadius(Game.Player.Character, crew.MarkerPosition, 40) && lineOfSight)
                {
                    Vector3 pos = new(crew.MarkerPosition.X, crew.MarkerPosition.Y,
                        crew.MarkerPosition.Z);
                    DrawTitle(pos, crew.CrewName + "\nLevel ~g~" + crew.CrewLevel);
                }

                // Handle member creation.
                if (IsEntityInsideRadius(Game.Player.Character, crew.MarkerPosition, MarkerXy))
                {
                    if (!Crew.AreAnyHired)
                    {
                        if (Game.IsControlJustPressed(Control.Context))
                        {
                            crew.SetCrewHired(true);

                            // Add player to crew.
                            Member playerMember = new(Game.Player.Character, crew);
                            crew.Owner = playerMember;

                            Screen.FadeOut(500);

                            Scaleform.ScaleformHandle =
                                Function.Call<int>(Hash.REQUEST_SCALEFORM_MOVIE, "mp_big_message_freemode");
                            while (!Function.Call<bool>(Hash.HAS_SCALEFORM_MOVIE_LOADED, Scaleform.ScaleformHandle))
                            {
                                Wait(0);
                            }

                            Function.Call<bool>(Hash.BEGIN_SCALEFORM_MOVIE_METHOD, Scaleform.ScaleformHandle,
                                "SHOW_SHARD_WASTED_MP_MESSAGE");
                            Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_TEXTURE_NAME_STRING, crew.CrewName);
                            Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_TEXTURE_NAME_STRING,
                                "Level: " + crew.CrewLevel);
                            Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, 5);
                            Function.Call(Hash.END_SCALEFORM_MOVIE_METHOD);
                            Wait(1000);

                            // TODO: Rework this. (or not)
                            // I'm doing this here to temporarily escape fixing the situation where dead peds giving out XP on crew hire.
                            foreach (Ped ped in World.GetAllPeds().Where(ped => ped.IsDead))
                            {
                                ped.Delete();
                            }

                            // Spawn the crew in.
                            for (int i = 0; i < Level.GetPedCount(crew.CrewLevel); i++)
                            {
                                PedHash pedHash = (PedHash) crew.ModelVariations[_random.Next(0, crew.ModelVariations.Count)];
                                Ped ped = World.CreatePed(pedHash, Game.Player.Character.Position);
                                Member member = new(ped, crew);
                                member.AssignAttributes();
                            }

                            Screen.FadeIn(500);
                            Scaleform.Enabled = true;
                        }

                        Screen.ShowHelpTextThisFrame("Press ~INPUT_CONTEXT~ to hire ~y~" + crew.CrewName);
                    }
                    else
                    {
                        Screen.ShowHelpTextThisFrame("You can only have one crew hired at a time.");
                    }
                }
            }
            else
            {
                // If the player is far away.
                if (crew.Placeholders.Count > 0)
                {
                    foreach (Ped ped in crew.Placeholders)
                    {
                        ped.Delete();
                    }

                    crew.Placeholders.Clear();
                    crew.HavePlaceholdersSpawned = false;
                }
            }


            if (crew.IsHired)
            {
                // Handle player death. (disband crew)
                if (Game.Player.Character.IsDead)
                {
                    crew.Disband();
                    Logger.Log("Crew disbanded.");
                    return;
                }

                // Before we do anything we need to actively check if the user changes his ped. If this is the case then we need to reassign
                // some things.
                if (!crew.ActiveMembers[0].Character.IsPlayer)
                {
                    crew.ActiveMembers[0].Character = Game.Player.Character;
                    crew.Owner = crew.ActiveMembers[0];
                    Logger.Log("Reassigning Player Ped.");
                }

                // Handle Members
                // Has to be declared every tick for dynamic detection.
                List<Member> activeMembers = crew.ActiveMembers.ToList();

                // Handle Active Members
                foreach (Member member in activeMembers)
                {
                    // If crew member dies, remove them from crew. This should also cover the peds if they don't exist (deleted by user)
                    if (member.Character.IsDead)
                    {
                        member.LeaveCrew();
                        member.Leader.Followers.Remove(member);
                        foreach (Member follower in member.Followers)
                        {
                            follower.AssignNewLeader(2);
                            Logger.Log("Assinging new leader.");
                        }

                        member.Followers.Clear(); // Clear excess (probably not needed)
                        Logger.Log("Removed member.");
                        Logger.Log(crew.ActiveMembers.Count);
                    }

                    // If all members are dead, remove crew from active crews.
                    // We put 1 here because the player should technically be the last one alive.
                    if (activeMembers.Count == 1)
                    {
                        crew.Disband();
                        Logger.Log("Removing Active Crew. All peds are dead. (excluding player)");
                        return;
                    }

                    // Handle Member Checkpoints. (useful for when members gets too far from leader and task breaks)
                    // TODO: Needs work, it sometimes this gets called every tick when a member's pathing gets stuck.
                    float distance = Vector3.Distance2D(member.Character.Position, member.Checkpoint);
                    if (distance >= 8)
                    {
                        member.Checkpoint = member.Character.Position;
                        member.HasCheckpointUpdated = true;
                    }

                    // Task Management
                    if (!member.Character.IsPlayer && member.Character.Exists())
                    {
                        if (!Game.Player.Character.IsInVehicle())
                        {
                            // This may or may not be needed (requires more testing)
                            if (member.VehicleGroup != null) member.VehicleGroup = null;

                            if (member.HasEnteredAssignedVehicle)
                            {
                                member.HasEnteredAssignedVehicle = false;
                                Logger.Log("entered assigned vehicle = false");
                            }

                            crew.Owner.State = MemberState.None;
                            int rndValue = _random.Next(2, 5);

                            float memberDistance = Vector3.Distance2D(member.Leader.Character.Position, member.Character.Position);
                            float playerDistance = Vector3.Distance2D(Game.Player.Character.Position, member.Character.Position);
                            if (playerDistance < 80)
                            {
                                if (memberDistance < 8 && member.State != MemberState.Walk)
                                {
                                    // Logger.Log($"{member.Character.Handle} Walk state.");
                                    member.State = MemberState.Walk;
                                    member.Character.Task.FollowToOffsetFromEntity(member.Leader.Character,
                                        member.Character.Position - member.Leader.Character.Position.Around(rndValue), 1.0f, distanceToFollow: 3.0f);
                                }

                                if (memberDistance is >= 8 and < 80 && member.State != MemberState.Run)
                                {
                                    // Logger.Log($"{member.Character.Handle} Run state.");
                                    member.State = MemberState.Run;
                                    member.Character.Task.FollowToOffsetFromEntity(member.Leader.Character,
                                        Vector3.Zero, 3.0f, distanceToFollow: 0.0f);
                                }
                            }
                            else
                            {
                                // This should probably be called outside this scope but putting it here seems reasonable considering this branch is where
                                // we're handling the player tasks on foot. Plus, the statement below only executes once besides looping through members.
                                if (crew.Owner.HasCheckpointUpdated)
                                {
                                    foreach (Member farMember in activeMembers.Where(m => m != crew.Owner))
                                    {
                                        farMember.State = MemberState.OutOfReach;
                                        Logger.Log($"Setting Member {farMember.Character.Handle} to walk to leader {crew.Owner.Character.Handle}");
                                        Vector3 pos = crew.Owner.Checkpoint;
                                        Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, farMember.Character, pos.X, pos.Y, pos.Z, 3.0f, -1, 0.0f, 0.0f);
                                    }

                                    crew.Owner.HasCheckpointUpdated = false;
                                }
                            }
                        }
                    }

                    // Handle Levels & Experience
                    // This is somehow getting called before my initial check of member.Character.IsDead.
                    // The initial idea is that activeMembers is supposed to remove the member before this gets called.
                    // TODO: Figure how why this is getting called before the member death check at the start of this member loop.
                    foreach (Ped ped in World.GetAllPeds().Where(ped => ped.IsDead))
                    {
                        if (ped.Killer == member.Character)
                        {
                            if (!crew.RegisteredPedKills.Contains(ped))
                            {
                                crew.RegisteredPedKills.Add(ped);

                                Member isPedMember = activeMembers.Find(m => m.Character == ped);
                                if (isPedMember != null)
                                {
                                    Logger.Log("Ped was a member, continuing.");
                                    continue;
                                }
                                
                                ped.MarkAsNoLongerNeeded();

                                int pedType = Function.Call<int>(Hash.GET_PED_TYPE, ped);
                                Logger.Log($"Member {member.Character.Handle} has killed a ped of type: " + (PedType) pedType);
                                switch (pedType)
                                {
                                    case (int) PedType.PedTypeCop:
                                        crew.AddExperience(XpEvent.KillCop);
                                        break;
                                    case (int) PedType.PedTypeSwat:
                                        crew.AddExperience(XpEvent.KillSwat);
                                        break;
                                    case (int) PedType.PedTypeArmy:
                                        crew.AddExperience(XpEvent.KillArmy);
                                        break;
                                    default:
                                        crew.AddExperience(XpEvent.KillPed);
                                        break;
                                }
                            }
                        }
                    }

                    // Used for development.
                    // member.DisplayHandles();
                }

                if (Game.Player.Character.IsInVehicle())
                {
                    if (crew.Owner.State != MemberState.DriveVehicle)
                    {
                        crew.Owner.State = MemberState.DriveVehicle;
                        Logger.Log("Updating CrewOwnerState");

                        Logger.Log("Looping through each vehicle");
                        List<Vehicle> vehicleList = World.GetAllVehicles().ToList();

                        int iterationCount = 1;
                        // Create a list copying over the elements of activeMembers to manipulate.
                        var remainingVehicleMembers = crew.ActiveMembers.Where(member => !member.Character.IsPlayer).ToList();

                        // TODO: Optimization - Break out of parent loop when exception is caught.
                        foreach (Vehicle vehicle in vehicleList.ToList())
                        {
                            bool isPlayerVehicle = Game.Player.Character.CurrentVehicle == vehicle;

                            // +1 to include the driver seat.
                            int passengerCapacity = isPlayerVehicle ? vehicle.PassengerCapacity : vehicle.PassengerCapacity + 1;
                            if (IsEntityInsideRadius(vehicle, Game.Player.Character.Position, SearchRange))
                            {
                                VehicleGroup vehicleGroup = new(vehicle);
                                // vehicle.IsPersistent = true;
                                Logger.Log("Vehicle Passenger Capacity " + passengerCapacity);
                                for (int i = 0; i < passengerCapacity; i++)
                                {
                                    try
                                    {
                                        VehicleSeat seat;

                                        // This should eventually cause an out of range exception.
                                        Member member = crew.ActiveMembers[iterationCount];
                                        if (!isPlayerVehicle)
                                        {
                                            seat = vehicleGroup.Seats.Dequeue();
                                            if (i == 0)
                                            {
                                                vehicleGroup.CurrentDriver = member;
                                            }
                                        }
                                        else
                                        {
                                            vehicleGroup.Seats.Dequeue(); // Remove the driver (since the player is the driver).
                                            seat = vehicleGroup.Seats.Dequeue();
                                        }

                                        // TODO: In the event that the vehicle gets out of range we need to prevent the members from infinitely chasing the vehicle.
                                        Logger.Log($"Setting member {member.Character.Handle} into seat " + seat);
                                        member.EnterVehicle(vehicle, seat);
                                        vehicleGroup.AddToGroup(member);
                                        remainingVehicleMembers.Remove(member);
                                        iterationCount++;

                                        Logger.Log("Members Left to Assign: " + remainingVehicleMembers.Count);
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Log("Out of bounds.");
                                        break;
                                    }
                                }

                                // TODO: Do something about the members who couldn't find a vehicle.
                                foreach (Member member in remainingVehicleMembers)
                                {
                                    // For now we'll just assign them to run after their leader.
                                    // Eventually we'll have logic to gradually check for vehicles around them as they're running.
                                    member.Character.Task.FollowToOffsetFromEntity(member.Leader.Character,
                                        Vector3.Zero, 3.0f, distanceToFollow: 0.0f);
                                    Logger.Log("This member has no vehicle: " + member.Character.Handle);
                                }

                                vehicleList.Remove(vehicle); // Remove vehicle since peds are already assigned to it.
                                Logger.Log(
                                    $"Vehicle {vehicle.Handle} is in radius: {IsEntityInsideRadius(vehicle, Game.Player.Character.Position, 25)} | {passengerCapacity}");
                            }
                        }
                    }

                    // Handle Vehicle Groups
                    foreach (Member member in activeMembers.Where(member => !member.Character.IsPlayer))
                    {
                        if (member.VehicleGroup == null) continue;

                        if (member.VehicleGroup.Vehicle.Driver == Game.Player.Character) continue;

                        if (member.Character.IsInVehicle())
                        {
                            member.Character.BlockPermanentEvents = true;

                            if (!member.HasEnteredAssignedVehicle)
                            {
                                member.VehicleGroup.EnteredCount++;
                                Logger.Log("Increasing Entered Count to " + member.VehicleGroup.EnteredCount);
                                member.HasEnteredAssignedVehicle = true;
                            }

                            if (member != member.VehicleGroup.CurrentDriver)
                            {
                                if (member.VehicleGroup.CurrentDriver.Character.IsDead)
                                {
                                    member.VehicleGroup.IsEscortActive = false;
                                    Ped passenger = member.VehicleGroup.Vehicle.GetPedOnSeat(VehicleSeat.Passenger);

                                    // For members that aren't in the passenger seat. (assuming there's no passenger)
                                    if (passenger == null)
                                    {
                                        Logger.Log("Passenger does not exist.");
                                        member.Character.SetIntoVehicle(member.Character.CurrentVehicle, VehicleSeat.Passenger);
                                        Logger.Log("Shuffling Seat.");
                                        member.Character.Task.ShuffleToNextVehicleSeat(member.Character.CurrentVehicle);
                                        member.VehicleGroup.CurrentDriver = member;
                                    }
                                    else
                                    {
                                        member.Character.Task.ShuffleToNextVehicleSeat(member.Character.CurrentVehicle);
                                        member.VehicleGroup.CurrentDriver = member;
                                        Logger.Log("Shuffling Seat.");
                                    }
                                }
                            }

                            // Assign shit to member if they're the driver.
                            if (member.VehicleGroup.EnteredCount == member.VehicleGroup.Members.Count)
                            {
                                // I'm going to prevent calling this native every tick for the sake of performance.
                                // int isEscortActive = Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, member.Character, 0xb41f1a34);

                                // I'm sure a boolean check is more performant despite how minuscule the difference it might be.

                                if (member == member.VehicleGroup.CurrentDriver && !member.VehicleGroup.IsEscortActive &&
                                    member.Character.CurrentVehicle.Driver == member.Character)
                                {
                                    // Remove handbrake
                                    // This check is really not needed since this branch only executes once,
                                    // however I'm keeping it here just in case some changes happen in the future with our parent statement.
                                    if (member.VehicleGroup.IsHandbrakeForced)
                                    {
                                        member.VehicleGroup.IsHandbrakeForced = false;
                                    }

                                    Logger.Log("Setting task to follow");
                                    member.VehicleGroup.IsEscortActive = true;
                                    Function.Call(Hash.TASK_VEHICLE_ESCORT, member.Character, member.Character.CurrentVehicle,
                                        Game.Player.Character.CurrentVehicle, -1,
                                        200f, 787004, 10.0f, 0, 15.0f);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (Member member in activeMembers)
                    {
                        member.Character.BlockPermanentEvents = false;
                    }
                }

                // Rewards and Levels
                if (Game.Player.WantedLevel == 0 && WantedLevelBeforeEvasion > 0)
                {
                    crew.AddExperience(XpEvent.EscapePolice);
                    WantedLevelBeforeEvasion = 0;
                }

                // This may or may not be too overpowered, depending on how the user plays.
                if (Vector3.Distance2D(Game.Player.Character.Position, crew.Checkpoint) >= (!Game.Player.Character.IsInVehicle() ? 200 : 600))
                {
                    crew.Checkpoint = Game.Player.Character.Position;
                    Logger.Log("Crew Checkpoint updated.");
                    crew.AddExperience(XpEvent.Checkpoint);
                }

                // TODO: The rank bar needs more work but I got it to be somewhat feasible.
                if (Level.CanLevelUp(crew.CrewLevel, crew.Experience))
                {
                    int savedXp = 0;
                    if (crew.Experience > Level.ProjectedExperience(crew.CrewLevel))
                        savedXp = crew.Experience - Level.ProjectedExperience(crew.CrewLevel);

                    if (crew.PreviousProjected == 0)
                        crew.PreviousProjected = Level.ProjectedExperience(crew.CrewLevel);

                    Scaleform.DisplayLevelUp(0, crew.PreviousProjected, 0, crew.PreviousProjected, crew.CrewLevel);
                    crew.IncreaseLevel();
                    crew.PreviousProjected = (Level.ProjectedExperience(crew.CrewLevel) - crew.Experience) + savedXp;
                    Scaleform.DisplayLevelUp(0, crew.PreviousProjected, 0, savedXp, crew.CrewLevel);

                    Logger.Log($"Crew has leveled up to {crew.CrewLevel} with {crew.Experience} XP");
                }
            }
        }

        if (ModelMenu.Camera != null)
        {
            if (ModelMenu.Camera.IsActive)
            {
                Game.DisableAllControlsThisFrame();
            }
        }

        // Vehicle Detection Radius for Members (visual aid)
        // World.DrawMarker(MarkerType.VerticalCylinder,
        //     new Vector3(Game.Player.Character.Position.X, Game.Player.Character.Position.Y,
        //         Game.Player.Character.Position.Z - Game.Player.Character.HeightAboveGround), Vector3.Zero, Vector3.Zero,
        //     new Vector3(SearchRange, SearchRange, 5f), Color.FromArgb(50, 255, 255, 255));
    }
}