using System;
using GTA;
using GTA.Math;
using LemonUI.Menus;

namespace LSCrews.Source.Menu.MarkerData;

public partial class MarkerMenu
{
    private void OnMarkerPositionActivated(object sender, EventArgs args)
    {
        Vector3 position = Game.Player.Character.Position;
        CrewMenu.TemporaryMarkerPos = new Vector3(position.X, position.Y, position.Z - 1f);
        SetCoordinates(new Vector3(position.X, position.Y, position.Z - 1f));
    }

    private void OnMarkerCoordChanged(object sender, ItemChangedEventArgs<float> args)
    {
        args.Object = args.Direction == Direction.Right ? args.Object + 0.1f : args.Object - 0.1f;
    }
}