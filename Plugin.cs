using System.IO;
using System.Collections.Generic;
using System.Reflection;

using LitJson;

using BepInEx;
using HarmonyLib;

namespace SubnauticaModloader
{
    [BepInPlugin("subnautica-modloader", "Subnautica Modloader", "0.0.1")]
    internal class Plugin : BaseUnityPlugin
    {
        private class JSONModInfoWrapper
        {
            public string Name = "";
            public string Description = "";
            public string Author = "";
            public string Version = "";
            public string AssemblyLocation = "";
            public ModInfo Unwrap()
            {
                return new ModInfo(Name, Description, Author, Version) { AssemblyLocation = AssemblyLocation };
            }
        }

        public static Plugin instance;
        public static readonly List<ModInfo> Mods = new List<ModInfo>();

        public static void LogWarning(string message)
        {
            instance.Logger.LogWarning(message);
        }
        public static void LogError(string message)
        {
            instance.Logger.LogError(message);
        }
        public static void LogMessage(string message)
        {
            instance.Logger.LogMessage(message);
        }
        public static void LogInfo(string message)
        {
            instance.Logger.LogInfo(message);
        }

        public void Awake()
        {
            if (instance != null)
            {
                LogError("Modloader plugin is already initialized.");
                return;
            };
            instance = this;

            Harmony harmony = new Harmony("subnautica-modloader");
            Tech.ApplyPatches(harmony);

            LogInfo("Loading mods...");
            foreach (string dir in Directory.GetDirectories("Mods\\"))
            {
                if (File.Exists(dir + "\\mod.json"))
                {
                    JSONModInfoWrapper modInfo = JsonMapper.ToObject<JSONModInfoWrapper>(File.ReadAllText(dir + "\\mod.json"));
                    if (!string.IsNullOrWhiteSpace(modInfo.Name))
                    {
                        ModInfo mod = modInfo.Unwrap();
                        mod.Directory = dir;
                        Mods.Add(mod);
                        LogInfo("Name: " + modInfo.Name);
                        LogInfo("Description: " + modInfo.Description);
                        LogInfo("Author: " + modInfo.Author);
                        LogInfo("Version: " + modInfo.Version + "\n");
                    }
                }
                else
                {
                    LogError("Failed to find mod.json for " + dir + ".");
                }
            }
            Tech.LoadJSON();
            for (int i = 0; i < Mods.Count; i ++)
            {
                if(!string.IsNullOrWhiteSpace(Mods[i].AssemblyLocation))
                {
                    Assembly assembly = Assembly.LoadFrom(Mods[i].Directory + "\\" + Mods[i].AssemblyLocation);
                    if(assembly != null)
                    {
                        MethodInfo main = assembly.GetType("Mod").GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                        if(main != null)
                        {
                            main.Invoke(null, new object[1] { i });
                        }
                        else
                        {
                            main = assembly.GetType("Mod").GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
                            if(main != null)
                            {
                                main.Invoke(null, new object[1] { i });
                            }
                            else
                            {
                                LogError("Failed to find entry point at " + Mods[i].Directory + "\\" + Mods[i].AssemblyLocation + ".");
                            }
                        }
                    }
                    else
                    {
                        LogError("Failed to load assembly of " + Mods[i].Name + ".");
                    }
                }
            }
        }
    }
}