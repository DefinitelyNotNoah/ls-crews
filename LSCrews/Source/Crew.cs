using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using LSCrews.Source.Menu;
using Newtonsoft.Json;

namespace LSCrews.Source;

public enum XpEvent
{
    KillPed,
    KillCop,
    KillSwat,
    KillArmy,
    EscapePolice,
    Checkpoint,
}

public class Crew
{
    public static readonly List<Crew> CrewList = new();

    public static Crew CurrentlyHired { get; set; }

    [JsonIgnore] public int PreviousProjected;

    [JsonIgnore] public Vector3 Checkpoint = Game.Player.Character.Position;
    
    [JsonIgnore] public bool IsHired;

    [JsonIgnore] public static bool AreAnyHired;

    [JsonIgnore] public Member Owner { get; set; }

    [JsonIgnore] public readonly List<Ped> Placeholders = new();

    [JsonIgnore] public bool HavePlaceholdersSpawned;

    [JsonIgnore] public List<Ped> RegisteredPedKills = new();

    [JsonIgnore] public Blip Blip { get; set; }

    [JsonIgnore] public readonly List<Member> ActiveMembers = new();
    
    public string CrewName;

    public readonly List<uint> ModelVariations = new();

    public Vector3 MarkerPosition = Vector3.Zero;

    public readonly List<(Vector3 position, float heading, uint hash)> PlaceholderModels = new();

    public int CrewLevel;

    public int Experience;

    public void SetCrewHired(bool toggle)
    {
        AreAnyHired = toggle;
        IsHired = toggle;
    }

    public void Disband()
    {
        SetCrewHired(false);
        foreach (Member member in ActiveMembers.ToList())
        {
            member.LeaveCrew();
        }

        CrewMenu.CurrentCrewMenu.CurrentCrewSubmenu.Back();
        CrewMenu.CurrentCrewMenu.CurrentCrewSubmenuItem.Enabled = false;
    }

    private void UpdateJson()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        Logger.Log("Attempting to write at " + StorageManager.CrewDirectory + CrewName + ".json");
        File.WriteAllText(StorageManager.CrewDirectory + CrewName + ".json", json);
        Logger.Log("Write successful");
    }

    public void IncreaseLevel()
    {
        CrewLevel++;
        Logger.Log($"Increasing level to {CrewLevel}");
        Logger.Log("Total Crew XP: " + Experience);
        Logger.Log("XP to Level Up: " + Level.ProjectedExperience(CrewLevel));
        UpdateJson();
    }

    private void IncreaseExperience(int amount)
    {
        Experience += amount;
        Logger.Log($"Increasing experience by {amount}.");
        Logger.Log("Total Crew XP: " + Experience);
        Logger.Log("XP to Level Up: " + Level.ProjectedExperience(CrewLevel));
        UpdateJson();
    }

    public void AddExperience(XpEvent xpEvent)
    {
        switch (xpEvent)
        {
            case XpEvent.KillPed:
                IncreaseExperience(1);
                break;
            case XpEvent.KillCop:
                IncreaseExperience(2);
                break;
            case XpEvent.KillSwat:
                IncreaseExperience(3);
                break;
            case XpEvent.KillArmy:
                IncreaseExperience(4);
                break;
            case XpEvent.EscapePolice:
                IncreaseExperience(Main.WantedLevelBeforeEvasion * 5);
                break;
            case XpEvent.Checkpoint:
                IncreaseExperience(1);
                break;
        }
    }
}