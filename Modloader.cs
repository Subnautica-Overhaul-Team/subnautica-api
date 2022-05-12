using System.Collections.Generic;

namespace SubnauticaModloader
{
    public class ModInfo
    {
        public string Name;
        public string Description;
        public string Author;
        public string Version;
        public string AssemblyLocation;

        public string Directory;

        public Dictionary<string, Tech> Tech = new Dictionary<string, Tech>();

        public ModInfo(string name, string description, string author, string version)
        {
            Name = name;
            Description = description;
            Author = author;
            Version = version;
        }
    }
    public class Modloader
    {
        public ModInfo GetModInfo(int index)
        {
            return Plugin.Mods[index];
        }
        public ModInfo GetModInfo(string name)
        {
            foreach(ModInfo mod in Plugin.Mods)
            {
                if(mod.Name == name)
                {
                    return mod;
                }
            }
            return null;
        }
    }
}
