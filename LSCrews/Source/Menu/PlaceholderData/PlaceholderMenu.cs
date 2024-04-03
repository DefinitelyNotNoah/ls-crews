using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
using LSCrews.Source.Menu.ModelData;

namespace LSCrews.Source.Menu.PlaceholderData;

public partial class PlaceholderMenu
{
    public static readonly List<PlaceholderMenu> Placeholders = new();
    
    public Ped Ped { get; set; }

    public NativeItem IdItem = new("ID")
    {
        Enabled = false,
        AltTitle = Placeholders.Count.ToString()
    };

    public NativeDynamicItem<float> XItem = new("Position X:")
    {
        ArrowsAlwaysVisible = true
    };

    public NativeDynamicItem<float> YItem = new("Position Y:")
    {
        ArrowsAlwaysVisible = true
    };

    public NativeDynamicItem<float> ZItem = new("Position Z:")
    {
        ArrowsAlwaysVisible = true
    };

    public NativeDynamicItem<float> HeadingItem = new("Heading");

    public NativeItem RemoveItem = new("Remove Item")
    {
        UseCustomBackground = true
    };

    public NativeMenu Menu = new("")
    {
        Name = $"Ped #{Placeholders.Count}"
    };

    public NativeSubmenuItem MenuItem { get; set; }

    public NativeItem PositionItem = new("Set to Current Location");

    public readonly NativeMenu SelectPlaceholderMenu = new("Select Model", "Select Model");

    public NativeSubmenuItem SelectPlaceholderMenuItem { get; set; }

    public static PlaceholderMenu Get(NativeMenu menu) => Placeholders.Find(p => p.IdItem == menu.Items[0]);

    public void Refresh(int startIndex)
    {
        for (int i = startIndex; i < Placeholders.Count; i++)
        {
            PlaceholderMenu placeholderMenu = Placeholders[i];
            Logger.Log(placeholderMenu.Menu);
            placeholderMenu.IdItem.AltTitle = (Convert.ToInt32(placeholderMenu.Menu.Items[0].AltTitle) - 1).ToString();
            placeholderMenu.Menu.Name = $"Ped #{placeholderMenu.IdItem.AltTitle}";
            placeholderMenu.MenuItem.Title = placeholderMenu.Menu.Name;
        }
    }

    public void Delete(PlaceholderMenu placeholderMenu)
    {
        int id = Placeholders.FindIndex(m => m == placeholderMenu);
        NativeItem item = placeholderMenu.Menu.Parent.SelectedItem;
        placeholderMenu.Menu.Back();
        placeholderMenu.Menu.Parent.Remove(item);
        placeholderMenu.Menu.Parent.SelectedIndex = 0;
        CrewMenu.CurrentCreateMenu.Pool.Remove(placeholderMenu.Menu);
        Placeholders.Remove(placeholderMenu);
        placeholderMenu.Ped.Delete();
        Refresh(id);
    }

    public void SetCoordinates(Vector3 position, float heading)
    {
        XItem.SelectedItem = position.X;
        YItem.SelectedItem = position.Y;
        ZItem.SelectedItem = position.Z;
        HeadingItem.SelectedItem = heading;
    }

    public static void SetPlaceholderAttributes(Ped ped)
    {
        ped.SetNoCollision(Game.Player.Character, false);
        ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;
        ped.IsInvincible = true;
        ped.CanWrithe = false;
        ped.CanRagdoll = false;
        ped.IsPositionFrozen = true; 
        ped.SetConfigFlag(13, true);
        ped.SetConfigFlag(17, true);
        ped.SetConfigFlag(39, true);
        ped.SetConfigFlag(128, false);
        ped.SetConfigFlag(208, true);
        ped.SetConfigFlag(294, true);
        ped.SetConfigFlag(430, true);
        ped.SetConfigFlag(456, false);
    }

    public PlaceholderMenu(NativeMenu menu)
    {
        MenuItem = menu.AddSubMenu(Menu);
        Menu.Add(IdItem);
        SelectPlaceholderMenuItem = Menu.AddSubMenu(SelectPlaceholderMenu);
        Menu.Add(PositionItem);
        Menu.Add(XItem);
        Menu.Add(YItem);
        Menu.Add(ZItem);
        Menu.Add(HeadingItem);
        RemoveItem.Colors.BackgroundNormal = Color.DarkRed;
        RemoveItem.Colors.BackgroundHovered = Color.Red;
        RemoveItem.Colors.TitleHovered = Color.White;
        Menu.Add(RemoveItem);
        CrewMenu.CurrentCreateMenu.Pool.Add(SelectPlaceholderMenu);

        foreach ((string name, Hash hash) model in ModelMenu.RegisteredModels)
        {
            NativeItem item = new(model.name, "", model.hash.ToString());
            item.Activated += OnPlaceholderModelActivated;
            SelectPlaceholderMenu.Add(item);
        }

        RemoveItem.Activated += OnPlaceholderRemoveActivated;
        PositionItem.Activated += OnPlaceholderPositionActivated;
        XItem.ItemChanged += OnPlaceholderCoordChanged;
        YItem.ItemChanged += OnPlaceholderCoordChanged;
        ZItem.ItemChanged += OnPlaceholderCoordChanged;
        HeadingItem.ItemChanged += OnPlaceholderHeadingChanged;
        Placeholders.Add(this);
    }
}