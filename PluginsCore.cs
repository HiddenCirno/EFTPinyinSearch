using BepInEx;
using HarmonyLib;
using System;

namespace EFTPinyinSearch
{
    [BepInPlugin(PluginsInfo.GUID, PluginsInfo.NAME, PluginsInfo.VERSION)]
    public class PluginsCore : BaseUnityPlugin
    {
        public void Awake()
        {
            Console.WriteLine($"[{PluginsInfo.NAME}] 插件已加载");
            var harmony = new Harmony(PluginsInfo.GUID);
            harmony.PatchAll();
        }
    }
}
