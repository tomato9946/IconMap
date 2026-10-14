using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using ModLoader;
using SFS.World;
using UnityEngine;

namespace RocketIconMod
{
    public class Main : Mod
    {
        public override string ModNameID => "LunaTomatoIcons";
        public override string DisplayName => "Icon Map";
        public override string Author => "Luna Tomato";
        public override string MinimumGameVersionNecessary => "1.5.9.8";
        public override string ModVersion => "v1.1.0";
        public override string Description =>
            "Tired of the same dull triangle on the map? This mod brings your rocket icons to life. Press G to cycle icons. Add your own icons with the Code and Texture folders next to the mod .dll.";

        public override string IconLink => "https://raw.githubusercontent.com/tomato9946/icon-sfs-/refs/heads/main/icon-icon%20map.png";

        public static readonly string[] IconNames = new string[]
        {
            "Default", "Satellite", "Space Station", "Base",
            "Debris", "Lander", "Rover", "Voyager",
            "Hubble", "Probe", "Question Mark",
            "Octagon Star", "Hexagon Star", "Square Star",
            "Flag", "Square", "Circle"
        };

        private static readonly Dictionary<string, string> ResourceMap = new Dictionary<string, string>
        {
            { "Satellite", "Satellite.png" },
            { "Space Station", "SpaceStation.png" },
            { "Base", "Base.png" },
            { "Debris", "Debris.png" },
            { "Lander", "Lander.png" },
            { "Rover", "Rover.png" },
            { "Voyager", "Voyager.png" },
            { "Hubble", "Hubble.png" },
            { "Probe", "Probe.png" },
            { "Question Mark", "QuestionMark.png" },
            { "Octagon Star", "OctagonStar.png" },
            { "Hexagon Star", "HexagonStar.png" },
            { "Square Star", "SquareStar.png" },
            { "Flag", "Flag.png" },
            { "Square", "Square.png" },
            { "Circle", "Circle.png" }
        };

        public static Dictionary<string, Sprite> LoadedIcons = new Dictionary<string, Sprite>();

        public static List<string> IconList = new List<string>();

        private static void BuildIconList()
        {
            IconList.Clear();
            foreach (string n in IconNames)
            {
                if (n == "Default" || LoadedIcons.ContainsKey(n)) IconList.Add(n);
            }
            foreach (string n in CustomIcons.Names)
            {
                if (!IconList.Contains(n) && LoadedIcons.ContainsKey(n)) IconList.Add(n);
            }
        }

        public static System.Runtime.CompilerServices.ConditionalWeakTable<Rocket, IconBox> IconByRocketInstance =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Rocket, IconBox>();
        public class IconBox { public string Name; }

        public static Dictionary<string, string> IconByName = new Dictionary<string, string>();

        public static System.Runtime.CompilerServices.ConditionalWeakTable<SFS.World.Maps.MapIcon, SpriteBox> OriginalSprites =
            new System.Runtime.CompilerServices.ConditionalWeakTable<SFS.World.Maps.MapIcon, SpriteBox>();
        public class SpriteBox { public Sprite Sprite; }

        public static Main Instance;
        public static SFS.World.Maps.MapRocket SelectedRocket = null;

        private static string ConfigPath;
        private const float IconPPU = 250f;

        public override void Load()
        {
            Instance = this;

            string saveFolder = Application.persistentDataPath;
            try { Directory.CreateDirectory(saveFolder); } catch { }
            ConfigPath = Path.Combine(saveFolder, "LunaTomatoIcons_rocket_icons.txt");

            LoadAllIconsFromEmbedded();
            CustomIcons.Load(ModFolder);
            BuildIconList();
            LoadConfig();

            new Harmony(ModNameID).PatchAll(Assembly.GetExecutingAssembly());
            SFSMod.Settings.Setup();
            IconInputHandler.EnsureExists();

        }

        private void LoadAllIconsFromEmbedded()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] allResources = asm.GetManifestResourceNames();

            foreach (var kv in ResourceMap)
            {
                string resourceName = null;
                foreach (string r in allResources)
                {
                    if (r.EndsWith(kv.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = r;
                        break;
                    }
                }
                if (resourceName == null) continue;

                try
                {
                    using (Stream stream = asm.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) continue;
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        Sprite sprite = CreateSpriteFromBytes(data);
                        if (sprite != null)
                        {
                            LoadedIcons[kv.Key] = sprite;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LunaTomatoIcons] Failed {kv.Key}: {e.Message}");
                }
            }
        }

        private Sprite CreateSpriteFromBytes(byte[] fileData)
        {
            return CreateSprite(fileData, 1f, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        }

        /// <summary>
        /// </summary>
        public static Sprite CreateSprite(byte[] fileData, float size, Vector2 pivot, FilterMode filter)
        {
            try
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var loadImageMethod = typeof(Texture2D).GetMethod(
                    "LoadImage", BindingFlags.Public | BindingFlags.Instance, null,
                    new Type[] { typeof(byte[]) }, null);

                if (loadImageMethod != null)
                    loadImageMethod.Invoke(texture, new object[] { fileData });
                else
                {
                    var t = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                    t?.GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Static, null,
                        new Type[] { typeof(Texture2D), typeof(byte[]) }, null)
                        ?.Invoke(null, new object[] { texture, fileData });
                }

                if (texture.width <= 2 && texture.height <= 2) return null;

                texture.filterMode = filter;
                float ppu = IconPPU * (texture.width / 512f) / Mathf.Max(0.05f, size);
                return Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    pivot,
                    ppu);
            }
            catch { return null; }
        }

        private const char TagMarker = '\u2063';
        private const char Bit0 = '\u200B';
        private const char Bit1 = '\u200C';

        private static string GenerateInvisibleTag()
        {
            long raw = DateTime.UtcNow.Ticks ^ ((long)Guid.NewGuid().GetHashCode() << 20);
            uint bits = unchecked((uint)raw);
            var sb = new StringBuilder();
            sb.Append(TagMarker);
            for (int i = 31; i >= 0; i--)
                sb.Append(((bits >> i) & 1) == 1 ? Bit1 : Bit0);
            return sb.ToString();
        }

        private static bool IsInvisibleTag(string name) =>
            !string.IsNullOrEmpty(name) && name[0] == TagMarker;

        public static string ResolveIconFor(Rocket rocket)
        {
            if (rocket == null) return "Default";

            if (IconByRocketInstance.TryGetValue(rocket, out IconBox box))
                return box.Name;

            string resolved = "Default";

            string name = rocket.rocketName;
            if (!string.IsNullOrEmpty(name) && IconByName.TryGetValue(name, out string byName))
                resolved = byName;

            IconByRocketInstance.Add(rocket, new IconBox { Name = resolved });
            return resolved;
        }

        public static void SelectRocket(SFS.World.Maps.MapRocket mr)
        {
            if (mr == null || mr.rocket == null) return;
            SelectedRocket = mr;
            string icon = ResolveIconFor(mr.rocket);
        }

        public static void CycleSelectedIcon()
        {
            if (SelectedRocket == null)
            {
                Debug.LogWarning("[LunaTomatoIcons] No rocket selected — click a rocket first");
                return;
            }
            if (SelectedRocket.rocket == null || SelectedRocket.mapIcon == null)
            {
                SelectedRocket = null;
                Debug.LogWarning("[LunaTomatoIcons] Selected rocket is gone");
                return;
            }

            Rocket rocket = SelectedRocket.rocket;

            string current = IconByRocketInstance.TryGetValue(rocket, out IconBox curBox) ? curBox.Name : "Default";
            int idx = IconList.IndexOf(current);
            if (idx < 0) idx = 0;

            string next = IconList.Count > 0 ? IconList[(idx + 1) % IconList.Count] : "Default";

            IconByRocketInstance.Remove(rocket);
            IconByRocketInstance.Add(rocket, new IconBox { Name = next });

            string name = rocket.rocketName;
            if (string.IsNullOrEmpty(name))
            {
                name = GenerateInvisibleTag();
                rocket.rocketName = name;
            }

            IconByName[name] = next;
            SaveConfig();

            ApplyIcon(SelectedRocket.mapIcon, next);
            ShowIconMessage(next);

            string label = IsInvisibleTag(name) ? "(unnamed)" : name;
        }

        /// <summary>
        /// </summary>
        public static void InheritIcon(Rocket from, Rocket to)
        {
            if (from == null || to == null) return;
            string icon = IconByRocketInstance.TryGetValue(from, out IconBox box) ? box.Name : "Default";
            IconByRocketInstance.Remove(to);
            IconByRocketInstance.Add(to, new IconBox { Name = icon });
        }

        public static void CleanupRocket(Rocket rocket)
        {
            if (rocket == null) return;
            IconByRocketInstance.Remove(rocket);
        }

        /// <summary>
        /// </summary>
        public static void ApplyIcon(SFS.World.Maps.MapIcon mi, string iconName)
        {
            if (mi == null || mi.mapIcon == null) return;
            var sr = mi.mapIcon.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;

            if (!OriginalSprites.TryGetValue(mi, out SpriteBox origBox))
            {
                origBox = new SpriteBox { Sprite = sr.sprite };
                OriginalSprites.Add(mi, origBox);
            }

            Color keepColor = sr.color;

            if (iconName == "Default" || !LoadedIcons.ContainsKey(iconName))
            {
                if (origBox.Sprite != null)
                    sr.sprite = origBox.Sprite;
            }
            else
            {
                sr.sprite = LoadedIcons[iconName];
            }

            sr.color = keepColor;
        }

        /// <summary>
        /// </summary>
        public static void ShowIconMessage(string iconName)
        {
            try { SFS.UI.MsgDrawer.main.Log("Icon: " + iconName); }
            catch (Exception) { }
        }

        public static void CleanupMapIcon(SFS.World.Maps.MapIcon mi)
        {
            if (mi == null) return;
            OriginalSprites.Remove(mi);
        }

        private static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return;
                }
                foreach (string line in File.ReadAllLines(ConfigPath, Encoding.UTF8))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string k = line.Substring(0, eq);
                    string v = line.Substring(eq + 1).Trim();

                    if (!IconList.Contains(v)) continue;
                    if (string.IsNullOrEmpty(k)) continue;

                    IconByName[k] = v;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LunaTomatoIcons] Failed to load log: {e.Message}\n{ConfigPath}");
            }
        }

        public static void SaveConfig()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in IconByName)
                    lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(ConfigPath, lines.ToArray(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LunaTomatoIcons] Failed to save log: {e.Message}\n{ConfigPath}");
            }
        }
    }
}
