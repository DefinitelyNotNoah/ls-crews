using System;
using GTA;
using GTA.Math;
using LemonUI.Menus;
using LSCrews.Source.Menu.ModelData;

namespace LSCrews.Source.Menu.PlaceholderData;

public partial class PlaceholderMenu
{
    private void OnPlaceholderRemoveActivated(object sender, EventArgs args)
    {
        PlaceholderMenu placeholderMenu = Get((NativeMenu)sender);
        Delete(placeholderMenu);
        CrewMenu.CurrentCrewMenu.UpdateConfirmColors();
    }

    private void OnPlaceholderPositionActivated(object sender, EventArgs args)
    {
        PlaceholderMenu placeholderMenu = Get((NativeMenu)sender);
        Logger.Log(((NativeMenu)sender).Name);
        Vector3 position = Game.Player.Character.Position;
        float heading = Game.Player.Character.Heading;
        placeholderMenu.SetCoordinates(new Vector3(position.X, position.Y, position.Z - 1f), heading);
        
        Logger.Log(ModelMenu.RegisteredModels.Count);
        Ped.Position = new Vector3(position.X, position.Y, position.Z - 1f);
        Ped.Heading = heading;
    }

    private void OnPlaceholderCoordChanged(object sender, ItemChangedEventArgs<float> args)
    {
        args.Object = args.Direction == Direction.Right ? args.Object + 0.1f : args.Object - 0.1f;
        Ped.Position = new Vector3(XItem.SelectedItem, YItem.SelectedItem, ZItem.SelectedItem);
    }

    private void OnPlaceholderHeadingChanged(object sender, ItemChangedEventArgs<float> args)
    {
        args.Object = args.Direction == Direction.Right ? args.Object + 5f : args.Object - 5f;
        Ped.Heading = HeadingItem.SelectedItem;
    }
    
    public void OnPlaceholderModelActivated(object sender, EventArgs args)
    {
        NativeMenu menu = (NativeMenu)sender;
        NativeItem item = menu.SelectedItem;
        Logger.Log(item.Title);
        Vector3 pos = new(Ped.Position.X, Ped.Position.Y, Ped.Position.Z - 1);
        float heading = Ped.Heading;
        Ped.Delete();
        CrewMenu.CurrentCrewMenu.TemporaryPlaceholders.Remove(Ped);
        Ped = World.CreatePed((PedHash)Convert.ToUInt32(item.AltTitle), pos, heading);
        SetPlaceholderAttributes(Ped);
        CrewMenu.CurrentCrewMenu.TemporaryPlaceholders.Add(Ped);
    }
}