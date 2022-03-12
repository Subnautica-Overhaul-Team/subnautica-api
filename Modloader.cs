namespace SubnauticaModloader
{
    class Modloader
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
