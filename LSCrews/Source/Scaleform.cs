using System;
using GTA;
using GTA.Native;
using GTA.UI;

namespace LSCrews.Source;

public class Scaleform : Script
{
    public static bool Enabled { get; set; }

    public static int ScaleformHandle { get; set; }

    public Scaleform()
    {
        Tick += OnTick;
    }

    public static void DisplayLevelUp(int limitStart, int limitEnd, int previousValue, int currentValue, int currentRank)
    {
        while (!Function.Call<bool>(Hash.HAS_SCALEFORM_SCRIPT_HUD_MOVIE_LOADED, 19))
        {
            Function.Call(Hash.REQUEST_SCALEFORM_SCRIPT_HUD_MOVIE, 19);
            Logger.Log("LOADING..");
            Wait(0);
        }
        Logger.Log("LOADED");
        
        Function.Call(Hash.BEGIN_SCALEFORM_SCRIPT_HUD_MOVIE_METHOD, 19, "SET_COLOUR");
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, 116); // https://docs.fivem.net/docs/game-references/hud-colors/
        Function.Call(Hash.END_SCALEFORM_MOVIE_METHOD);

        Function.Call(Hash.BEGIN_SCALEFORM_SCRIPT_HUD_MOVIE_METHOD, 19, "SET_RANK_SCORES");
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, limitStart);
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, limitEnd);
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, previousValue);
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, currentValue);
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, currentRank);
        Function.Call(Hash.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT, 100);
        Function.Call(Hash.END_SCALEFORM_MOVIE_METHOD);
    }

    private void OnTick(object sender, EventArgs args)
    {
        if (Enabled)
        {
            Function.Call(Hash.DRAW_SCALEFORM_MOVIE_FULLSCREEN, ScaleformHandle, 255, 255, 255, 255);
        }
    }
}