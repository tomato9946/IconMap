# IconMap

Custom rocket map icons for [Spaceflight Simulator](https://store.steampowered.com/app/1718870/) (PC)

Press **G** to cycle icons. Add your own icons with the `Code` and `Texture` folders next to the mod DLL.

**Author:** Luna Tomato  
**Game:** SFS 1.5.9.8+

## Install

1. Build the project (or download a release).
2. Copy `IconMap.dll` into:
   ```
   .../Spaceflight Simulator Game/Mods/IconMap/
   ```
3. Optional: put custom icon definitions in `Mods/IconMap/Code/` and images in `Mods/IconMap/Texture/`.

## Build

1. Copy game Managed DLLs into `Dependencies/` (from `Spaceflight Simulator_Data/Managed`):
   - `0Harmony.dll`, `Assembly-CSharp.dll`, `UnityEngine*.dll`, etc.
2. Copy `IconMap/env.props.example` to `IconMap/env.props` and set `ModsFolder` if you want auto-copy.
3. Open `IconMap.sln` in Visual Studio.
4. Build **Release**.

`AutoCopyToMods` defaults to **false** so the build does not fail when the game has the DLL locked (MSB3021).  
To enable auto-copy, set `<AutoCopyToMods>true</AutoCopyToMods>` in `env.props` and close the game before building.

## Custom icons

`Code/MyIcon.txt` example:

```text
Texture=MyIcon.png
Size=1
```

Place `MyIcon.png` in the `Texture` folder.

## License

MIT (or your preferred license).
