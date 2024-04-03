using System;
using System.ComponentModel;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
using LSCrews.Source.Menu.PlaceholderData;

namespace LSCrews.Source.Menu.ModelData;

public partial class ModelMenu
{
    public static void OnMenuOpening(object sender, CancelEventArgs args)
    {
        Logger.Log("Opening");
        SavedPed = Game.Player.Character;
        Vector3 pos = SavedPed.Position;
        SavedPed.IsVisible = false;
        SavedPed.IsInvincible = true;

        string modelName = ((NativeMenu)sender).SelectedItem.AltTitle;
        Hash hash = Function.Call<Hash>(Hash.GET_HASH_KEY, modelName);
        TempPed = World.CreatePed((PedHash)hash, new Vector3(pos.X, pos.Y, pos.Z - 1), SavedPed.Heading);
        Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, TempPed, true, true);

        // Camera
        Vector3 position = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS, TempPed,
            0.0f, 1.5f,
            1f);
        Camera = World.CreateCamera(position, Vector3.Zero, 60.0f);
        Function.Call(Hash.RENDER_SCRIPT_CAMS, true, true, 500, true, true);
    }

    public static void OnModelMenuIndexChanged(object sender, SelectedEventArgs args)
    {
        string modelName = ((NativeMenu)sender).Items[args.Index].AltTitle;
        Hash item = Function.Call<Hash>(Hash.GET_HASH_KEY, modelName);
        bool modelChangeSuccess = Game.Player.ChangeModel((PedHash)item);
        if (modelChangeSuccess)
        {
            TempPed = Game.Player.Character;
            TempPed.IsInvincible = true;
            Camera.PointAt(TempPed);
            Logger.Log(TempPed.IsInvincible);
            TempPed.SetNoCollision(SavedPed, true);
            Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, Game.Player.Character, true);
        }

        Logger.Log("Index Changed");
    }

    public static void OnMenuClosing(object sender, CancelEventArgs args)
    {
        Logger.Log("Closing");
        SavedPed.IsVisible = true;
        SavedPed.IsInvincible = false;

        World.RenderingCamera.Delete();
        Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, 500, true, true);

        Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, SavedPed, true, true);
        TempPed.Delete();
    }

    public static void OnModelItemActivated(object sender, EventArgs args)
    {
        NativeItem activatedItem = ((NativeMenu)sender).SelectedItem;
        Hash hash = Function.Call<Hash>(Hash.GET_HASH_KEY, activatedItem.AltTitle);
        NativeItem item = new(activatedItem.Title, "", hash.ToString());
        
        if (!RegisteredModels.Contains((activatedItem.Title, hash)))
        {
            RegisteredModels.Add((activatedItem.Title, hash));
            activatedItem.UseCustomBackground = true;
            activatedItem.Colors.BackgroundNormal = Color.Green;
            activatedItem.Colors.BackgroundHovered = Color.Green;
            activatedItem.Colors.TitleHovered = Color.Black;
            activatedItem.UpdateColors();
            Logger.Log(RegisteredModels.Count);

            // Update placeholder model select menu.
            foreach (PlaceholderMenu placeholder in PlaceholderMenu.Placeholders)
            {
                item.Activated += placeholder.OnPlaceholderModelActivated;
                placeholder.SelectPlaceholderMenu.Add(item);
                Logger.Log("Adding Model from " + placeholder.Menu.Name);
            }
        }
        else
        {
            RegisteredModels.Remove((activatedItem.Title, hash));
            activatedItem.UseCustomBackground = false;
            activatedItem.UpdateColors();
            Logger.Log(RegisteredModels.Count);

            // Update placeholder model select menu.
            foreach (PlaceholderMenu placeholder in PlaceholderMenu.Placeholders)
            {
                int index = placeholder.SelectPlaceholderMenu.Items.FindIndex(i => i.Title == item.Title);
                placeholder.SelectPlaceholderMenu.Remove(placeholder.SelectPlaceholderMenu.Items[index]);
            }
        }

        CrewMenu.CurrentCreateMenu.SetPlaceholderSubmenuItem.Enabled = RegisteredModels.Count > 0;
    }
}