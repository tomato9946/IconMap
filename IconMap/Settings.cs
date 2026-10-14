using ModLoader;
using RocketIconMod;
using SFS.Input;
using UnityEngine;

namespace SFSMod
{
    public class Settings : ModKeybindings
    {
        public static Settings Main;
        public KeybindingsPC.Key CycleIconKey = KeyCode.G;

        public static void Setup()
        {
            Main = SetupKeybindings<Settings>(RocketIconMod.Main.Instance);
        }

        public override void CreateUI()
        {
            Settings defaults = new Settings();
            base.CreateUI_Text("Custom Rocket Icons — by Luna Tomato");
            base.CreateUI_Space();
            base.CreateUI_Keybinding(CycleIconKey, defaults.CycleIconKey, "Cycle Icon Key");
            base.CreateUI_Space();
            base.CreateUI_Text("1. Click a rocket on the map");
            base.CreateUI_Text("2. Press G to cycle that rocket's icon");
            base.CreateUI_Text("Icons survive staging and separation.");
            base.CreateUI_Text("Name your rocket to keep its icon after restarting the game.");
            base.CreateUI_Space();
            base.CreateUI_Text("Add your own icons: put .txt files in the Code folder and images in the Texture folder next to the mod .dll.");
        }
    }
}
