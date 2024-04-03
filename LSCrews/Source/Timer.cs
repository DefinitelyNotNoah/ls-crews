using System;
using GTA;

namespace LSCrews.Source;

// TODO: Rework using GET_GAME_TIMER (or do some other math to figure out delta time).
// Using Wait() is reliant on frames per second and is not an accurate representation.
public class Timer : Script
{
    public Timer()
    {
        Tick += OnTick;
    }

    private void OnTick(object sender, EventArgs args)
    {
        if (Scaleform.Enabled)
        {
            Wait(3200);
            Scaleform.Enabled = false;
        }
    }
}