using System;
using System.Reflection;
using HarmonyLib;
using SFS.Input;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace RocketIconMod
{
    [HarmonyPatch(typeof(MapPlayer), "Start")]
    public static class MapPlayer_Start_Patch
    {
        static void Postfix(MapPlayer __instance)
        {
            if (!(__instance is MapRocket mr)) return;
            if (mr.rocket == null || mr.mapIcon == null) return;

            string icon = Main.ResolveIconFor(mr.rocket);
            Main.ApplyIcon(mr.mapIcon, icon);
        }
    }

    [HarmonyPatch(typeof(RocketManager), "CreateRocket_Child")]
    public static class RocketManager_CreateRocketChild_Patch
    {
        static void Postfix(Rocket __result, JointGroup jointGroup, Rocket parentRocket, Vector2 offset)
        {
            Main.InheritIcon(parentRocket, __result);
        }
    }

    [HarmonyPatch(typeof(Rocket), "OnDestroy")]
    public static class Rocket_OnDestroy_Patch
    {
        static void Postfix(Rocket __instance)
        {
            Main.CleanupRocket(__instance);
        }
    }

    [HarmonyPatch(typeof(MapIcon), "OnDestroy")]
    public static class MapIcon_OnDestroy_Patch
    {
        static void Postfix(MapIcon __instance)
        {
            Main.CleanupMapIcon(__instance);
        }
    }

    public class IconInputHandler : MonoBehaviour
    {
        private static MethodInfo _getMouseButtonDown;
        private static PropertyInfo _mousePosition;

        void Update()
        {
            try
            {
                EnsureInput();

                if (_getMouseButtonDown != null)
                {
                    bool click = (bool)_getMouseButtonDown.Invoke(null, new object[] { 0 });
                    if (click)
                    {
                        MapRocket hit = FindNearestMapRocket(60f);
                        if (hit != null)
                            Main.SelectRocket(hit);
                    }
                }

                bool cycle = false;
                var settings = SFSMod.Settings.Main;
                if (settings != null)
                    cycle = IsKeyDown(settings.CycleIconKey);

                if (cycle)
                    Main.CycleSelectedIcon();
            }
            catch { }
        }

        private static bool IsKeyDown(KeybindingsPC.Key key)
        {
            if (key == null) return false;
            try
            {
                if (key is I_Key ikey)
                    return ikey.IsKeyDown();
            }
            catch { }
            return false;
        }

        private void EnsureInput()
        {
            if (_getMouseButtonDown != null) return;

            Type inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule")
                          ?? Type.GetType("UnityEngine.Input, UnityEngine")
                          ?? typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.Input");

            if (inputType == null) return;
            _getMouseButtonDown = inputType.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
            _mousePosition = inputType.GetProperty("mousePosition");
        }

        private MapRocket FindNearestMapRocket(float maxPixelDist)
        {
            MapRocket[] all = UnityEngine.Resources.FindObjectsOfTypeAll<MapRocket>();
            if (all == null || all.Length == 0) return null;

            Camera cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null) return null;

            Vector3 mouse = _mousePosition != null ? (Vector3)_mousePosition.GetValue(null) : Vector3.zero;

            MapRocket best = null;
            float bestDist = maxPixelDist;

            foreach (MapRocket mr in all)
            {
                if (mr == null || mr.mapIcon == null || mr.mapIcon.mapIcon == null) continue;
                if (!mr.gameObject.activeInHierarchy) continue;

                Vector3 sp = cam.WorldToScreenPoint(mr.mapIcon.mapIcon.transform.position);
                if (sp.z < 0) continue;

                float d = Vector2.Distance(new Vector2(mouse.x, mouse.y), new Vector2(sp.x, sp.y));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = mr;
                }
            }
            return best;
        }

        public static void EnsureExists()
        {
            if (GameObject.Find("LunaTomatoIcons_Input") != null) return;
            var go = new GameObject("LunaTomatoIcons_Input");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<IconInputHandler>();
        }
    }
}
