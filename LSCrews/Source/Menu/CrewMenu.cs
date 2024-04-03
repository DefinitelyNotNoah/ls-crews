using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using LSCrews.Source.Menu.MarkerData;
using LSCrews.Source.Menu.ModelData;
using LSCrews.Source.Menu.PlaceholderData;
using Newtonsoft.Json;

namespace LSCrews.Source.Menu;

// TODO: Perform checks to make sure directories exist before accessing parts of the menu just in case players delete it while in-game.

public class CrewMenu
{
    // Object Pool
    public readonly ObjectPool Pool = new();

    // Main Menu + List of Crews
    public readonly NativeMenu LandingMenu = new("LSCrews", "Select an Option", "Test Description");

    private readonly NativeMenu _listOfCrews = new("List of Crews", "List of Crews");

    private readonly NativeItem _deleteCrewFromListItem = new("Delete Crew");

    private readonly NativeMenu _createCrewSubmenu = new("Create a Crew", "Create a Crew");

    // Create Menu
    private readonly NativeItem _setCrewNameItem = new("Set Crew Name");

    private readonly NativeMenu _setModelVariationsSubmenu = new("Modal Variations", "Set Model Variations");

    private NativeSubmenuItem SetModelVariationsSubMenuItem { get; set; }

    private readonly NativeMenu _setMarkersSubmenu = new("Markers", "Set Marker");
    private NativeSubmenuItem SetMarkersSubmenuItem { get; set; }
    private MarkerMenu Marker { get; set; }

    private readonly NativeMenu _setPlaceholderSubmenu = new("Stationary Peds", "Set Stationary Peds");
    
    public NativeSubmenuItem SetPlaceholderSubmenuItem { get; set; }

    private readonly NativeItem _addPlaceholderItem = new("Add a Stationary Ped", "Only 3 peds can be added.");


    private readonly NativeItem _confirmItem = new("Confirm Settings");

    private bool CanConfirm { get; set; }

    public static Vector3 TemporaryMarkerPos { get; set; } = Vector3.Zero;

    public List<Ped> TemporaryPlaceholders { get; } = new();
    private NativeSubmenuItem CreateCrewSubmenuItem { get; set; }

    // Current CrewMenu Instance
    public static CrewMenu CurrentCreateMenu { get; set; }

    public CrewMenu()
    {
        // Handle Main  & List of Crews
        LandingMenu.RotateCamera = true;
        LandingMenu.AddSubMenu(_listOfCrews);
        CreateCrewSubmenuItem = LandingMenu.AddSubMenu(_createCrewSubmenu);

        LandingMenu.Closed += OnLandingMenuClosed;
        _deleteCrewFromListItem.Activated += OnDeleteCrewFromListItemActivated;

        _deleteCrewFromListItem.UseCustomBackground = true;
        _deleteCrewFromListItem.Colors.BackgroundNormal = Color.DarkRed;
        _deleteCrewFromListItem.Colors.BackgroundHovered = Color.Red;
        _deleteCrewFromListItem.Colors.TitleHovered = Color.White;

        Pool.Add(LandingMenu);
        Pool.Add(_listOfCrews);
        Pool.Add(_createCrewSubmenu);

        // Handle Create Menu Item Assignment
        SetModelVariationsSubMenuItem = _createCrewSubmenu.AddSubMenu(_setModelVariationsSubmenu);
        SetMarkersSubmenuItem = _createCrewSubmenu.AddSubMenu(_setMarkersSubmenu);
        SetPlaceholderSubmenuItem = _createCrewSubmenu.AddSubMenu(_setPlaceholderSubmenu);
        _createCrewSubmenu.Add(_setCrewNameItem);
        _createCrewSubmenu.Add(_confirmItem);

        _setPlaceholderSubmenu.Add(_addPlaceholderItem);
        SetPlaceholderSubmenuItem.Enabled = ModelMenu.RegisteredModels.Count > 0;
        _addPlaceholderItem.Enabled = PlaceholderMenu.Placeholders.Count < 3;
        Marker = new MarkerMenu(_setMarkersSubmenu);
        

        _confirmItem.UseCustomBackground = true;
        _confirmItem.Colors.BackgroundNormal = Color.DarkRed;
        _confirmItem.Colors.BackgroundHovered = Color.Red;
        _confirmItem.Colors.TitleHovered = Color.White;
        _confirmItem.UpdateColors();

        // Assign events
        _addPlaceholderItem.Activated += OnAddPlaceholderItemActivated;
        _setCrewNameItem.Activated += OnSetCrewNameItemActivated;
        _confirmItem.Activated += OnConfirmItemActivated;

        Pool.Add(_setModelVariationsSubmenu);
        Pool.Add(_setMarkersSubmenu);
        Pool.Add(_setPlaceholderSubmenu);

        CurrentCreateMenu = this;

        ModelMenu.AddAllModelItems(_setModelVariationsSubmenu);
    }
    
    public void OnAddPlaceholderItemActivated(object sender, EventArgs args)
    {
        switch (PlaceholderMenu.Placeholders.Count)
        {
            case 3:
                return;
            default:
                Logger.Log("Adding Placeholder Item Activated.");
                ((NativeMenu)sender).SelectedItem.Enabled = true;
                NativeMenu menu = (NativeMenu)sender;
                PlaceholderMenu placeholderMenu = new(menu);

                Vector3 playerPos = Game.Player.Character.Position;
                Vector3 position = new(playerPos.X, playerPos.Y, playerPos.Z - 1f);
                float heading = Game.Player.Character.Heading;
                placeholderMenu.SetCoordinates(position, heading);

                placeholderMenu.Ped = World.CreatePed((PedHash)ModelMenu.RegisteredModels[0].hash, position, heading);
                PlaceholderMenu.SetPlaceholderAttributes(placeholderMenu.Ped);
                TemporaryPlaceholders.Add(placeholderMenu.Ped);

                Pool.Add(placeholderMenu.Menu);

                _addPlaceholderItem.Enabled = PlaceholderMenu.Placeholders.Count < 3;
                UpdateConfirmColors();
                return;
        }
    }
    
    public void UpdateConfirmColors()
    {
        CanConfirm = PlaceholderMenu.Placeholders.Count > 0 && _setCrewNameItem.AltTitle.Length > 0;
        _confirmItem.Colors.BackgroundNormal = CanConfirm ? Color.DarkGreen : Color.DarkRed;
        _confirmItem.Colors.BackgroundHovered = CanConfirm ? Color.Green : Color.Red;
        _confirmItem.UpdateColors();
    }
    
    private void OnSetCrewNameItemActivated(object sender, EventArgs args)
    {
        string input = Game.GetUserInput();
        _setCrewNameItem.AltTitle = input;
        UpdateConfirmColors();
    }

    private void OnConfirmItemActivated(object sender, EventArgs args)
    {
        if (!CanConfirm) return;

        Crew crew = new()
        {
            CrewName = _setCrewNameItem.AltTitle
        };

        foreach ((string name, Hash hash) model in ModelMenu.RegisteredModels)
        {
            crew.ModelVariations.Add((uint)model.hash);
        }

        Vector3 markerPos = new(Marker.XItem.SelectedItem, Marker.YItem.SelectedItem, Marker.ZItem.SelectedItem);
        crew.MarkerPosition = markerPos;

        foreach (PlaceholderMenu placeholder in PlaceholderMenu.Placeholders)
        {
            Vector3 pos = new(placeholder.XItem.SelectedItem, placeholder.YItem.SelectedItem,
                placeholder.ZItem.SelectedItem);
            crew.PlaceholderModels.Add((pos, placeholder.HeadingItem.SelectedItem,
                (uint)placeholder.Ped.Model.Hash));
        }

        Crew.CrewList.Add(crew);

        string json = JsonConvert.SerializeObject(crew, Formatting.Indented, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        
        // Make sure directories exist.
        StorageManager.EstablishAllDirectories();
        File.WriteAllText(StorageManager.CrewDirectory + _setCrewNameItem.AltTitle + ".json", json);

        TemporaryMarkerPos = Vector3.Zero;

        // Remove all peds
        foreach (Ped ped in TemporaryPlaceholders)
        {
            ped.Delete();
            Logger.Log("Deleting Ped.");
        }

        TemporaryPlaceholders.Clear();
        PlaceholderMenu.Placeholders.Clear();
        ModelMenu.RegisteredModels.Clear();

        Logger.Log("Reassigning");
        ((NativeMenu)sender).Back();
        CurrentCreateMenu = new CrewMenu();

        // Add updated list of crews.
        Logger.Log("CONFIRM ITEM FINISHED");
        Logger.Log("Calling UpdateListOfCrews");
        CurrentCreateMenu.UpdateListOfCrews();
    }

    public void UpdateListOfCrews()
    {
        // Add existing crews to list menu.
        foreach (Crew crew in Crew.CrewList)
        {
            Vector3 markerPos = crew.MarkerPosition;
            Logger.Log("THE CREW NAME IS " + crew.CrewName);
            NativeMenu crewSubmenu = new(crew.CrewName, crew.CrewName);

            using OutputArgument arg = new();
            using OutputArgument arg2 = new();

            Function.Call(Hash.GET_STREET_NAME_AT_COORD, markerPos.X, markerPos.Y, markerPos.Z, arg, arg2);
            Hash streetHash = arg.GetResult<Hash>();
            string streetName = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY, streetHash);
            NativeItem crewLocationItem = new("Location: ", "", streetName);

            crewSubmenu.Add(crewLocationItem);
            crewSubmenu.Add(_deleteCrewFromListItem);
            _listOfCrews.AddSubMenu(crewSubmenu);
            Pool.Add(crewSubmenu);
        }
    }

    private void OnDeleteCrewFromListItemActivated(object sender, EventArgs args)
    {
        NativeMenu menu = (NativeMenu)sender;
        Crew crew = Crew.CrewList.Find(hq => hq.CrewName == menu.Name);
    
        if (crew.IsHired)
        {
            Logger.Log("Cannot delete crew while hired.");
            return;
        }

        menu.Back();

        Logger.Log("Deleting Crew.");
        Logger.Log("Here's the current HQ List Size: " + Crew.CrewList.Count);

        // Update CrewsList
        crew.Blip.Delete();

        // Remove Peds
        foreach (Ped ped in crew.Placeholders)
        {
            ped.Delete();
            Logger.Log("Deleting Ped: " + ped.Handle);
        }

        Crew.CrewList.Remove(crew);
        Logger.Log("Here's the updated HQ List Size: " + Crew.CrewList.Count);

        // Delete menu from list of crews.
        menu.Parent.Remove(menu.Parent.SelectedItem);

        StorageManager.MoveFileToDeleted(crew.CrewName + ".json");
    }

    private void OnLandingMenuClosed(object sender, EventArgs args) => Logger.Log("CLOSED (unused for now)");
}