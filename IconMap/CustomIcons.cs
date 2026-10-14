using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RocketIconMod
{
    //
    public static class CustomIcons
    {
        public static string RootFolder = "";
        public static string TextureFolder = "";
        public static string CodeFolder = "";

        public static readonly List<string> Names = new List<string>();

        public static void Load(string modFolder)
        {
            Names.Clear();

            RootFolder = ResolveRoot(modFolder);
            CodeFolder = Path.Combine(RootFolder, "Code");
            TextureFolder = Path.Combine(RootFolder, "Texture");

            try { Directory.CreateDirectory(CodeFolder); } catch { }
            try { Directory.CreateDirectory(TextureFolder); } catch { }

            // Single DLL: create example text files in Code/ and Texture/ if missing (no images)
            EnsureExampleFiles();

            string[] files;
            try { files = Directory.GetFiles(CodeFolder, "*.txt"); }
            catch (Exception e)
            {
                Debug.LogWarning("[LunaTomatoIcons] Could not read Code folder: " + CodeFolder + " | " + e.Message);
                return;
            }
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            int loaded = 0;
            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(name) || name.StartsWith("_")) continue;
                if (TryLoadOne(file, name)) loaded++;
            }
        }

        private static string ResolveRoot(string modFolder)
        {
            try
            {
                if (!string.IsNullOrEmpty(modFolder) && Directory.Exists(modFolder)) return modFolder;
            }
            catch { }
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(dir)) return dir;
            }
            catch { }
            return Application.persistentDataPath;
        }

        private static bool TryLoadOne(string codeFile, string name)
        {
            if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[LunaTomatoIcons] Name \"Default\" is reserved; skipped: " + codeFile);
                return false;
            }

            string texture = null;
            float size = 1f;
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            FilterMode filter = FilterMode.Bilinear;

            try
            {
                foreach (string raw in File.ReadAllLines(codeFile, Encoding.UTF8))
                {
                    string line = raw.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;

                    int sep = line.IndexOf(':');
                    int eq = line.IndexOf('=');
                    if (sep < 0 || (eq >= 0 && eq < sep)) sep = eq;
                    if (sep <= 0) continue;

                    string key = line.Substring(0, sep).Trim().ToLowerInvariant();
                    string val = line.Substring(sep + 1).Trim();

                    switch (key)
                    {
                        case "texture": texture = val; break;
                        case "size":
                            {
                                float f;
                                if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out f) && f > 0f)
                                    size = Mathf.Clamp(f, 0.05f, 20f);
                                break;
                            }
                        case "pivot":
                            {
                                string[] p = val.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                float px, py;
                                if (p.Length >= 2
                                    && float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out px)
                                    && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out py))
                                    pivot = new Vector2(Mathf.Clamp01(px), Mathf.Clamp01(py));
                                break;
                            }
                        case "filter":
                            filter = val.ToLowerInvariant() == "point" ? FilterMode.Point : FilterMode.Bilinear;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LunaTomatoIcons] Could not read code file: " + codeFile + " | " + e.Message);
                return false;
            }

            if (string.IsNullOrEmpty(texture))
            {
                Debug.LogWarning("[LunaTomatoIcons] " + name + ": missing Texture: line, skipped");
                return false;
            }

            string texPath = FindTexture(texture);
            if (texPath == null)
            {
                Debug.LogWarning("[LunaTomatoIcons] " + name + ": texture not found \"" + texture + "\" in " + TextureFolder);
                return false;
            }

            Sprite sprite;
            try { sprite = Main.CreateSprite(File.ReadAllBytes(texPath), size, pivot, filter); }
            catch (Exception e)
            {
                Debug.LogWarning("[LunaTomatoIcons] " + name + ": could not load image | " + e.Message);
                return false;
            }
            if (sprite == null)
            {
                Debug.LogWarning("[LunaTomatoIcons] " + name + ": could not create sprite (bad image or not png/jpg)");
                return false;
            }

            bool alreadyBundled = Array.IndexOf(Main.IconNames, name) >= 0;
            Main.LoadedIcons[name] = sprite;
            if (!alreadyBundled && !Names.Contains(name)) Names.Add(name);

            return true;
        }

        private static string FindTexture(string value)
        {
            try
            {
                string root = Path.GetFullPath(TextureFolder);
                string[] tries = Path.HasExtension(value)
                    ? new[] { value }
                    : new[] { value + ".png", value + ".jpg", value + ".jpeg" };

                foreach (string t in tries)
                {
                    string full = Path.GetFullPath(Path.Combine(TextureFolder, t.Replace('/', Path.DirectorySeparatorChar)));
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                    if (File.Exists(full)) return full;
                }
            }
            catch { }
            return null;
        }

        // On first load: create example help files next to the .dll if missing
        // Does not overwrite existing files (player edits are kept)
        private static void EnsureExampleFiles()
        {
            try
            {
                // Edit the example text below: ExampleCodeText / ExampleReadmeText
                WriteTextIfMissing(Path.Combine(CodeFolder, "_Example.txt"), ExampleCodeText);
                WriteTextIfMissing(Path.Combine(TextureFolder, "_README.txt"), ExampleReadmeText);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LunaTomatoIcons] failed to create example files: " + e.Message);
            }
        }

        private static void WriteTextIfMissing(string path, string content)
        {
            if (File.Exists(path)) return;
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private const string ExampleCodeText =
@"# Example code file — filenames starting with _ are NOT loaded
# How to use: copy this file and rename it to your icon name, e.g. MyRocket.txt
#   - 1 file = 1 icon
#   - The filename (without .txt) is the name shown when you press G to cycle icons, e.g. MyRocket
#   - Custom icons are added after the built-in ones, sorted by filename
#   - If the name matches a built-in icon (e.g. Flag.txt), it replaces that icon's image
#   - The name Default is reserved for the original triangle
# One setting per line, format:  Name: value

# [Required] Image filename in the Texture folder (png or jpg; extension optional)
Texture: MyRocket.png

# Size on the map: 1 = same as built-in icons, 2 = twice as large, 0.5 = half. Default 1
Size: 1

# Image pivot 0–1 (0.5, 0.5 = center). Default 0.5, 0.5
Pivot: 0.5, 0.5

# Bilinear = smooth / Point = sharp pixels (good for pixel art). Default Bilinear
Filter: Bilinear
";

        private const string ExampleReadmeText =
@"Put image files (png or jpg) for custom icons in this folder.
Then create a .txt file in the Code folder that points to the image with:
  Texture: your_image_name
(See Code\_Example.txt for a full example.)
Images with no matching Code file are ignored.
Recommended: transparent background, square size e.g. 512x512

----- Quick steps -----
1. Put your image here, e.g. MyShip.png
2. In the Code folder, copy _Example.txt and rename it to MyShip.txt
3. Open MyShip.txt and set:  Texture: MyShip.png
4. Restart the game → click a rocket on the map → press G
";
    }
}
