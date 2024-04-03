using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI.Menus;

namespace LSCrews.Source.Menu.MarkerData;

public partial class MarkerMenu
{
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

    public NativeItem PositionItem = new("Set to Current Location");

    public void SetCoordinates(Vector3 position)
    {
        XItem.SelectedItem = position.X;
        YItem.SelectedItem = position.Y;
        ZItem.SelectedItem = position.Z;
    }

    public MarkerMenu(NativeMenu menu)
    {
        menu.Add(PositionItem);
        menu.Add(XItem);
        menu.Add(YItem);
        menu.Add(ZItem);
        PositionItem.Activated += OnMarkerPositionActivated;
        XItem.ItemChanged += OnMarkerCoordChanged;
        YItem.ItemChanged += OnMarkerCoordChanged;
        ZItem.ItemChanged += OnMarkerCoordChanged;
    }
}