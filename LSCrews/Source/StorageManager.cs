using System;
using System.IO;
using System.Linq;
using GTA.UI;
using Newtonsoft.Json;

namespace LSCrews.Source;

public abstract class StorageManager
{
    public static readonly string MainDirectory = $"{Directory.GetCurrentDirectory()}/scripts/LSCrews/";
    public static readonly string CrewDirectory = $"{MainDirectory}crews/";
    private static readonly string DeletedDirectory = $"{CrewDirectory}/deleted/";
    private static readonly string LogFile = $"{MainDirectory}log.txt";
    
    public static void MoveFileToDeleted(string fileName)
    {
        // Establish directories again in case some are missing.
        EstablishAllDirectories();
        
        string fileDir = CrewDirectory + fileName;
        string deletedDir = DeletedDirectory + fileName;
        if (!File.Exists(fileDir)) return;

        if (!File.Exists(deletedDir))
        {
            File.Move(fileDir, deletedDir);
        }
        else
        {
            var files = Directory.GetFiles(DeletedDirectory).Where(file => file == deletedDir);
            File.Move(fileDir, deletedDir + files.Count());
        }
    }

    public static void EstablishAllDirectories()
    {
        // EstablishDirectory(MainDirectory); <-- Removing this to make sure people know they deleted the LS Crews folder (in the event it happens)
        EstablishDirectory(CrewDirectory);
        EstablishDirectory(DeletedDirectory);
        EstablishFile(LogFile);
    }

    private static void EstablishDirectory(string dir)
    {
        if (Directory.Exists(dir)) return;

        // Logger.Log("Creating directory at: " + dir);

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception e)
        {
            Notification.Show($"Error creating directory at {dir}.\n${e}");
            Logger.Log(e);
        }
    }

    private static void EstablishFile(string dir)
    {
        if (File.Exists(dir)) return;

        // Logger.Log("Creating file at: " + dir);

        try
        {
            File.Create(dir);
        }
        catch (Exception e)
        {
            Notification.Show($"Error creating file at {dir}.\n${e}");
            Logger.Log(e);
        }
    }

    public static void UpdateCrews(string dir)
    {
        foreach (string file in Directory.GetFiles(dir).ToList())
        {
            if (!file.EndsWith(".json")) continue;

            try
            {
                string text = File.ReadAllText(file);
                Crew crew = JsonConvert.DeserializeObject<Crew>(text);
                Logger.Log("Adding Headquarters: " + crew.CrewName);
                Crew.CrewList.Add(crew);
                
                Logger.Log("CrewHQList Size: " + Crew.CrewList.Count);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }
    }

    public static int GetFileCount(string dir) => Directory.GetFiles(dir).ToList().Count;
}