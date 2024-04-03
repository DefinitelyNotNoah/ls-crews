using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using GTA;
using GTA.Native;
using LemonUI.Menus;

namespace LSCrews.Source.Menu.ModelData;

public partial class ModelMenu
{
    public static List<(string name, Hash hash)> RegisteredModels = new();

    private NativeMenu Menu = new("");
    
    public NativeSubmenuItem MenuItem { get; set; }

    private static Ped SavedPed { get; set; }

    private static Ped TempPed { get; set; }

    public static Camera Camera { get; set; }
    
    public static void AddAllModelItems(NativeMenu menu)
    {
        string path = "./scripts/LSCrews/PedList.xml";
        XmlSerializer serializer = new(typeof(PedList));
        using StreamReader reader = new(path);
        PedList pedList = (PedList)serializer.Deserialize(reader);

        foreach (Category category in pedList.Categories)
        {
            ModelMenu modelMenu = new()
            {
                Menu = new NativeMenu(category.name, category.name),
            };
            
            foreach (CategoryPed ped in category.Peds)
            {
                NativeItem modelItem = new(ped.caption, "", ped.name);
                modelItem.Activated += OnModelItemActivated;
                modelMenu.Menu.Add(modelItem);
            }
             
            CrewMenu.CurrentCreateMenu.Pool.Add(modelMenu.Menu);
            modelMenu.MenuItem = menu.AddSubMenu(modelMenu.Menu);
            modelMenu.Menu.SelectedIndexChanged += OnModelMenuIndexChanged;
            modelMenu.Menu.Closing += OnMenuClosing;
            modelMenu.Menu.Opening += OnMenuOpening;
        }
    }
}