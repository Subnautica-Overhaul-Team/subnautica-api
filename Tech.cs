using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using LitJson;

using UWE;

using HarmonyLib;

namespace SubnauticaModloader
{
    public class Tech
    {
        private class JSONIngredientWrapper
        {
            public int ID = 0;
            public int Amount = 0;
        }
        private class JSONTechWrapper
        {
            public string ID = "";
            public int CraftType = 1;

            public int EquipmentSlot = 0;
            public int UseType = 0;

            public string Group = "Resources";
            public string Category = "BasicMaterials";

            public float CraftingTime = 2f;
            public int CraftAmount = 1;
            public int[] LinkedItems = new int[0];
            public int[] UnlockDependencies = new int[0];

            public int[] Size = new int[2] { 1, 1 };

            [NonSerialized]
            public List<JSONIngredientWrapper> Recipe = new List<JSONIngredientWrapper>();

            public string PrefabID = "";

            public string IconName = "";
            public string IconPath = "";

            private static List<TechType> ConvertToTechTypeList(int[] array)
            {
                List<TechType> list = new List<TechType>();
                foreach (int tech in array)
                {
                    list.Add((TechType)tech);
                }
                return list;
            }

            private Dictionary<TechType, int> ConvertRecipe()
            {
                Dictionary<TechType, int> recipe = new Dictionary<TechType, int>();
                foreach (JSONIngredientWrapper ingredient in Recipe)
                {
                    recipe.Add((TechType)ingredient.ID, ingredient.Amount);
                }
                return recipe;
            }

            public Tech Unwrap()
            {
                return new Tech(ID)
                {
                    CraftType = (CraftTree.Type)CraftType,
                    EquipmentSlot = (EquipmentType)EquipmentSlot,
                    UseType = (QuickSlotType)UseType,
                    Group = Group,
                    Category = Category,
                    CraftingTime = CraftingTime,
                    CraftAmount = CraftAmount,
                    LinkedItems = ConvertToTechTypeList(LinkedItems),
                    UnlockDependencies = ConvertToTechTypeList(UnlockDependencies),
                    InventorySize = new Vector2int(Size[0], Size[1]),
                    Recipe = ConvertRecipe(),
                    PrefabID = PrefabID,
                    IconName = IconName,
                    Icon = Utils.LoadIcon(IconPath),
                };
            }
        }

        private static bool prefabCacheInitialized = false;

        private static readonly Dictionary<TechType, EquipmentType> equipmentTypesData = (Dictionary<TechType, EquipmentType>)typeof(CraftData).GetField("equipmentTypes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, QuickSlotType> slotTypesData = (Dictionary<TechType, QuickSlotType>)typeof(CraftData).GetField("slotTypes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, CraftData.BackgroundType> backgroundData = (Dictionary<TechType, CraftData.BackgroundType>)typeof(CraftData).GetField("backgroundTypes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, float> craftingTimesData = (Dictionary<TechType, float>)typeof(CraftData).GetField("craftingTimes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, Vector2int> techSizesData = (Dictionary<TechType, Vector2int>)typeof(CraftData).GetField("itemSizes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly object recipeData = typeof(CraftData).GetField("techData", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        private static readonly Dictionary<TechType, string> stringsNormalData = (Dictionary<TechType, string>)typeof(TechTypeExtensions).GetField("stringsNormal", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, string> stringsLowercaseData = (Dictionary<TechType, string>)typeof(TechTypeExtensions).GetField("stringsLowercase", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<string, TechType> techTypesNormalData = (Dictionary<string, TechType>)typeof(TechTypeExtensions).GetField("techTypesNormal", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<string, TechType> techTypesIgnoreCaseData = (Dictionary<string, TechType>)typeof(TechTypeExtensions).GetField("techTypesIgnoreCase", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, string> techTypeKeysData = (Dictionary<TechType, string>)typeof(TechTypeExtensions).GetField("techTypeKeys", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<string, TechType> keyTechTypesData = (Dictionary<string, TechType>)typeof(TechTypeExtensions).GetField("keyTechTypes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<TechType, string> techTypeTooltipStringsData = (Dictionary<TechType, string>)typeof(CachedEnumString<TechType>).GetField("valueToString", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TooltipFactory.techTypeTooltipStrings);
        private static readonly Dictionary<SpriteManager.Group, Dictionary<string, Atlas.Sprite>> iconGroupsData = (Dictionary<SpriteManager.Group, Dictionary<string, Atlas.Sprite>>)typeof(SpriteManager).GetField("groups", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static readonly Dictionary<string, string> languageStringsData = (Dictionary<string, string>)typeof(Language).GetField("strings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Language.main);

        private static readonly Dictionary<string, GameObject> scenePrefabsData = (Dictionary<string, GameObject>)typeof(ScenePrefabDatabase).GetField("scenePrefabs", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        private static readonly Type TechData = typeof(CraftData).GetNestedType("TechData", BindingFlags.NonPublic);
        private static readonly Type Ingredients = typeof(CraftData).GetNestedType("Ingredients", BindingFlags.NonPublic);

        internal static void LoadJSON()
        {
            for (int i = 0; i < Plugin.Mods.Count; i++)
            {
                if (Directory.Exists(Plugin.Mods[i].Directory + "\\Tech"))
                {
                    foreach (string dir in Directory.GetDirectories(Plugin.Mods[i].Directory + "\\Tech"))
                    {
                        string js = File.ReadAllText(dir + "\\tech.json");
                        JSONTechWrapper jsTech = JsonMapper.ToObject<JSONTechWrapper>(js);
                        foreach (string file in Directory.GetFiles(dir + "\\Ingredients"))
                        {
                            js = File.ReadAllText(file);
                            JSONIngredientWrapper jsIngredient = JsonMapper.ToObject<JSONIngredientWrapper>(js);
                            jsTech.Recipe.Add(jsIngredient);
                        }
                        Add(i, jsTech.Unwrap());
                    }
                }
            }
        }
        public static void Add(int modIndex, Tech tech)
        {
            equipmentTypesData[tech.Type] = tech.EquipmentSlot;
            slotTypesData[tech.Type] = tech.UseType;
            backgroundData[tech.Type] = tech.Background;
            craftingTimesData[tech.Type] = tech.CraftingTime;
            techSizesData[tech.Type] = tech.InventorySize;

            stringsNormalData[tech.Type] = tech.ID;
            stringsLowercaseData[tech.Type] = tech.ID.ToLower();
            techTypesNormalData[tech.ID] = tech.Type;
            techTypesIgnoreCaseData[tech.ID.ToLower()] = tech.Type;
            techTypeKeysData[tech.Type] = tech.ID;
            keyTechTypesData[tech.ID] = tech.Type;

            techTypeTooltipStringsData[tech.Type] = "Tooltip_" + tech.ID;

            if (tech.Icon != SpriteManager.defaultSprite && tech.IconName == "")
            {
                iconGroupsData[SpriteManager.Group.Item][tech.ID] = tech.Icon;
            }
            else
            {
                iconGroupsData[SpriteManager.Group.Item][tech.ID] = SpriteManager.Get(SpriteManager.Group.Item, tech.IconName);
            }

            var techData = Activator.CreateInstance(TechData);
            var ingredients = Activator.CreateInstance(Ingredients);

            TechData.GetField("_craftAmount").SetValue(techData, tech.CraftAmount);
            TechData.GetField("_linkedItems").SetValue(techData, tech.LinkedItems);
            foreach (var ingredient in tech.Recipe)
            {
                Ingredients.GetMethod("Add", new Type[2] { typeof(TechType), typeof(int) }).Invoke(ingredients, new object[2] { ingredient.Key, ingredient.Value });
            }
            TechData.GetField("_ingredients").SetValue(techData, ingredients);
            recipeData.GetType().GetMethod("Add").Invoke(recipeData, new object[2] { tech.Type, techData });

            tech.Setup?.Invoke();
            Plugin.Mods[modIndex].Tech.Add(tech.ID, tech);
            Plugin.LogInfo("Added tech from " + Plugin.Mods[modIndex].Name + " mod to database. ID: " + tech.ID + ", TechType: " + tech.Type.ToString() + ".");
        }
        public static void ModifyEquipmentSlot(TechType type, EquipmentType eqslot)
        {
            equipmentTypesData[type] = eqslot;
        }
        public static void ModifyUseType(TechType type, QuickSlotType usetype)
        {
            slotTypesData[type] = usetype;
        }
        public static void ModifyBackground(TechType type, CraftData.BackgroundType bgtype)
        {
            backgroundData[type] = bgtype;
        }
        public static void ModifyCraftingTime(TechType type, float crtime)
        {
            craftingTimesData[type] = crtime;
        }
        public static void ModifyCraftAmount(TechType type, int amount)
        {
            object techData = recipeData.GetType().GetProperty("Item").GetValue(recipeData, new object[1] { type });
            techData.GetType().GetField("_craftAmount", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(techData, amount);
        }
        public static void ModifyLinkedItems(TechType type, List<TechType> ltechs)
        {
            object techData = recipeData.GetType().GetProperty("Item").GetValue(recipeData, new object[1] { type });
            techData.GetType().GetField("_linkedItems", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(techData, ltechs);
        }
        public static void ModifyItemSize(TechType type, Vector2int size)
        {
            techSizesData[type] = size;
        }
        public static void ModifyRecipe(TechType type, Dictionary<TechType, int> recipe)
        {
            var techData = Activator.CreateInstance(TechData);
            var ingredients = Activator.CreateInstance(Ingredients);

            foreach (var ingredient in recipe)
            {
                Ingredients.GetMethod("Add", new Type[2] { typeof(TechType), typeof(int) }).Invoke(ingredients, new object[2] { ingredient.Key, ingredient.Value });
            }
            TechData.GetField("_ingredients").SetValue(techData, ingredients);
            recipeData.GetType().GetProperty("Item").SetValue(recipeData, techData, new object[1] { type });
        }

        private static void KnownTech_Initialize_Postfix()
        {
            HashSet<TechType> defaultTechData = (HashSet<TechType>)typeof(KnownTech).GetField("defaultTech", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            List<KnownTech.CompoundTech> compoundTechData = (List<KnownTech.CompoundTech>)typeof(KnownTech).GetField("compoundTech", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (ModInfo mod in Plugin.Mods)
            {
                foreach (Tech i in mod.Tech.Values)
                {
                    if (i.UnlockDependencies.Count == 0)
                    {
                        defaultTechData.Add(i.Type);
                    }
                    else
                    {
                        KnownTech.CompoundTech ct = new KnownTech.CompoundTech()
                        {
                            techType = i.Type,
                            dependencies = i.UnlockDependencies
                        };
                        compoundTechData.Add(ct);
                    }
                }
            }
        }
        private static void PrefabDatabase_LoadPrefabDatabase_Postfix()
        {
            foreach (ModInfo mod in Plugin.Mods)
            {
                foreach (Tech i in mod.Tech.Values)
                {
                    PrefabDatabase.prefabFiles[i.ID] = i.ID;
                }
            }
        }
        private static void CraftData_PreparePrefabIDCache_Postfix()
        {
            if (!prefabCacheInitialized)
            {
                Dictionary<TechType, string> techMappingData = (Dictionary<TechType, string>)typeof(CraftData).GetField("techMapping", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

                foreach (ModInfo mod in Plugin.Mods)
                {
                    foreach (Tech i in mod.Tech.Values)
                    {
                        GameObject go;
                        if (!String.IsNullOrWhiteSpace(i.PrefabID))
                        {
                            if (PrefabDatabase.GetPrefabAsync(i.PrefabID).TryGetPrefab(out go))
                            {
                                go = GameObject.Instantiate(go);
                            }
                        }
                        else
                        {
                            go = i.Prefab;
                        }
                        techMappingData[i.Type] = i.ID;
                        scenePrefabsData[i.ID] = go;
                    }
                }

                prefabCacheInitialized = true;
            }
        }
        private static bool CraftTree_FabricatorScheme_Prefix(ref CraftNode __result)
        {
            Dictionary<string, Dictionary<string, CraftNode>> craftNodes = new Dictionary<string, Dictionary<string, CraftNode>>()
            {
                {
                    "Resources", new Dictionary<string, CraftNode>()
                    {
                        {
                            "BasicMaterials", new CraftNode("BasicMaterials", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("Titanium", TreeAction.Craft, TechType.Titanium),
                                new CraftNode("TitaniumIngot", TreeAction.Craft, TechType.TitaniumIngot),
                                new CraftNode("FiberMesh", TreeAction.Craft, TechType.FiberMesh),
                                new CraftNode("Silicone", TreeAction.Craft, TechType.Silicone),
                                new CraftNode("Glass", TreeAction.Craft, TechType.Glass),
                                new CraftNode("Bleach", TreeAction.Craft, TechType.Bleach),
                                new CraftNode("Lubricant", TreeAction.Craft, TechType.Lubricant),
                                new CraftNode("EnameledGlass", TreeAction.Craft, TechType.EnameledGlass),
                                new CraftNode("PlasteelIngot", TreeAction.Craft, TechType.PlasteelIngot)
                            })
                        },
                        {
                            "AdvancedMaterials", new CraftNode("AdvancedMaterials", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("HydrochloricAcid", TreeAction.Craft, TechType.HydrochloricAcid),
                                new CraftNode("Benzene", TreeAction.Craft, TechType.Benzene),
                                new CraftNode("AramidFibers", TreeAction.Craft, TechType.AramidFibers),
                                new CraftNode("Aerogel", TreeAction.Craft, TechType.Aerogel),
                                new CraftNode("Polyaniline", TreeAction.Craft, TechType.Polyaniline),
                                new CraftNode("HatchingEnzymes", TreeAction.Craft, TechType.HatchingEnzymes)
                            })
                        },
                        {
                            "Electronics", new CraftNode("Electronics", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("CopperWire", TreeAction.Craft, TechType.CopperWire),
                                new CraftNode("Battery", TreeAction.Craft, TechType.Battery),
                                new CraftNode("PrecursorIonBattery", TreeAction.Craft, TechType.PrecursorIonBattery),
                                new CraftNode("PowerCell", TreeAction.Craft, TechType.PowerCell),
                                new CraftNode("PrecursorIonPowerCell", TreeAction.Craft, TechType.PrecursorIonPowerCell),
                                new CraftNode("ComputerChip", TreeAction.Craft, TechType.ComputerChip),
                                new CraftNode("WiringKit", TreeAction.Craft, TechType.WiringKit),
                                new CraftNode("AdvancedWiringKit", TreeAction.Craft, TechType.AdvancedWiringKit),
                                new CraftNode("ReactorRod", TreeAction.Craft, TechType.ReactorRod)
                            })
                        }
                    }
                },
                {
                    "Survival", new Dictionary<string, CraftNode>()
                    {
                        {
                            "Water", new CraftNode("Water", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("FilteredWater", TreeAction.Craft, TechType.FilteredWater),
                                new CraftNode("DisinfectedWater", TreeAction.Craft, TechType.DisinfectedWater)
                            })
                        },
                        {
                            "CookedFood", new CraftNode("CookedFood", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("CookedHoleFish", TreeAction.Craft, TechType.CookedHoleFish),
                                new CraftNode("CookedPeeper", TreeAction.Craft, TechType.CookedPeeper),
                                new CraftNode("CookedBladderfish", TreeAction.Craft, TechType.CookedBladderfish),
                                new CraftNode("CookedGarryFish", TreeAction.Craft, TechType.CookedGarryFish),
                                new CraftNode("CookedHoverfish", TreeAction.Craft, TechType.CookedHoverfish),
                                new CraftNode("CookedReginald", TreeAction.Craft, TechType.CookedReginald),
                                new CraftNode("CookedSpadefish", TreeAction.Craft, TechType.CookedSpadefish),
                                new CraftNode("CookedBoomerang", TreeAction.Craft, TechType.CookedBoomerang),
                                new CraftNode("CookedLavaBoomerang", TreeAction.Craft, TechType.CookedLavaBoomerang),
                                new CraftNode("CookedEyeye", TreeAction.Craft, TechType.CookedEyeye),
                                new CraftNode("CookedLavaEyeye", TreeAction.Craft, TechType.CookedLavaEyeye),
                                new CraftNode("CookedOculus", TreeAction.Craft, TechType.CookedOculus),
                                new CraftNode("CookedHoopfish", TreeAction.Craft, TechType.CookedHoopfish),
                                new CraftNode("CookedSpinefish", TreeAction.Craft, TechType.CookedSpinefish)
                            })
                        },
                        {
                            "CuredFood", new CraftNode("CuredFood", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("CuredHoleFish", TreeAction.Craft, TechType.CuredHoleFish),
                                new CraftNode("CuredPeeper", TreeAction.Craft, TechType.CuredPeeper),
                                new CraftNode("CuredBladderfish", TreeAction.Craft, TechType.CuredBladderfish),
                                new CraftNode("CuredGarryFish", TreeAction.Craft, TechType.CuredGarryFish),
                                new CraftNode("CuredHoverfish", TreeAction.Craft, TechType.CuredHoverfish),
                                new CraftNode("CuredReginald", TreeAction.Craft, TechType.CuredReginald),
                                new CraftNode("CuredSpadefish", TreeAction.Craft, TechType.CuredSpadefish),
                                new CraftNode("CuredBoomerang", TreeAction.Craft, TechType.CuredBoomerang),
                                new CraftNode("CuredLavaBoomerang", TreeAction.Craft, TechType.CuredLavaBoomerang),
                                new CraftNode("CuredEyeye", TreeAction.Craft, TechType.CuredEyeye),
                                new CraftNode("CuredLavaEyeye", TreeAction.Craft, TechType.CuredLavaEyeye),
                                new CraftNode("CuredOculus", TreeAction.Craft, TechType.CuredOculus),
                                new CraftNode("CuredHoopfish", TreeAction.Craft, TechType.CuredHoopfish),
                                new CraftNode("CuredSpinefish", TreeAction.Craft, TechType.CuredSpinefish)
                            })
                        }
                    }
                },
                {
                    "Personal", new Dictionary<string, CraftNode>()
                    {
                        {
                            "Equipment", new CraftNode("Equipment", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("Tank", TreeAction.Craft, TechType.Tank),
                                new CraftNode("DoubleTank", TreeAction.Craft, TechType.DoubleTank),
                                new CraftNode("Fins", TreeAction.Craft, TechType.Fins),
                                new CraftNode("RadiationSuit", TreeAction.Craft, TechType.RadiationSuit),
                                new CraftNode("ReinforcedDiveSuit", TreeAction.Craft, TechType.ReinforcedDiveSuit),
                                new CraftNode("Stillsuit", TreeAction.Craft, TechType.Stillsuit),
                                new CraftNode("FirstAidKit", TreeAction.Craft, TechType.FirstAidKit),
                                new CraftNode("FireExtinguisher", TreeAction.Craft, TechType.FireExtinguisher),
                                new CraftNode("Rebreather", TreeAction.Craft, TechType.Rebreather),
                                new CraftNode("Compass", TreeAction.Craft, TechType.Compass),
                                new CraftNode("Pipe", TreeAction.Craft, TechType.Pipe),
                                new CraftNode("PipeSurfaceFloater", TreeAction.Craft, TechType.PipeSurfaceFloater),
                                new CraftNode("PrecursorKey_Purple", TreeAction.Craft, TechType.PrecursorKey_Purple),
                                new CraftNode("PrecursorKey_Blue", TreeAction.Craft, TechType.PrecursorKey_Blue),
                                new CraftNode("PrecursorKey_Orange", TreeAction.Craft, TechType.PrecursorKey_Orange)
                            })
                        },
                        {
                            "Tools", new CraftNode("Tools", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("Scanner", TreeAction.Craft, TechType.Scanner),
                                new CraftNode("Welder", TreeAction.Craft, TechType.Welder),
                                new CraftNode("Flashlight", TreeAction.Craft, TechType.Flashlight),
                                new CraftNode("Knife", TreeAction.Craft, TechType.Knife),
                                new CraftNode("DiveReel", TreeAction.Craft, TechType.DiveReel),
                                new CraftNode("AirBladder", TreeAction.Craft, TechType.AirBladder),
                                new CraftNode("Flare", TreeAction.Craft, TechType.Flare),
                                new CraftNode("Builder", TreeAction.Craft, TechType.Builder),
                                new CraftNode("LaserCutter", TreeAction.Craft, TechType.LaserCutter),
                                new CraftNode("StasisRifle", TreeAction.Craft, TechType.StasisRifle),
                                new CraftNode("PropulsionCannon", TreeAction.Craft, TechType.PropulsionCannon),
                                new CraftNode("LEDLight", TreeAction.Craft, TechType.LEDLight)
                            })
                        },
                        {
                            "Machines", new CraftNode("Machines", TreeAction.Expand, TechType.None).AddNode(new CraftNode[]
                            {
                                new CraftNode("Seaglide", TreeAction.Craft, TechType.Seaglide),
                                new CraftNode("Constructor", TreeAction.Craft, TechType.Constructor),
                                new CraftNode("Beacon", TreeAction.Craft, TechType.Beacon),
                                new CraftNode("SmallStorage", TreeAction.Craft, TechType.SmallStorage),
                                new CraftNode("Gravsphere", TreeAction.Craft, TechType.Gravsphere),
                                new CraftNode("CyclopsDecoy", TreeAction.Craft, TechType.CyclopsDecoy)
                            })
                        }
                    }
                }
            };

            foreach (ModInfo mod in Plugin.Mods)
            {
                foreach (Tech i in mod.Tech.Values)
                {
                    if (i.CraftType == CraftTree.Type.Fabricator)
                    {
                        if (craftNodes.TryGetValue(i.Group, out Dictionary<string, CraftNode> category))
                        {
                            if (category.TryGetValue(i.Category, out CraftNode node))
                            {
                                node.AddNode(new CraftNode(i.ID, TreeAction.Craft, i.Type));
                            }
                            else
                            {
                                craftNodes[i.Group][i.Category] = new CraftNode(i.Category, TreeAction.Expand, TechType.None).AddNode(new CraftNode(i.ID, TreeAction.Craft, i.Type));
                            }
                        }
                        else
                        {
                            craftNodes[i.Group] = new Dictionary<string, CraftNode>
                            {
                                [i.Category] = new CraftNode(i.Category, TreeAction.Expand, TechType.None).AddNode(new CraftNode(i.ID, TreeAction.Craft, i.Type))
                            };
                        }
                    }
                }
            }

            CraftNode root = new CraftNode("Root", TreeAction.None, TechType.None);
            foreach (var group in craftNodes)
            {
                CraftNode groupNode = new CraftNode(group.Key, TreeAction.Expand, TechType.None);
                foreach (var category in group.Value)
                {
                    groupNode.AddNode(category.Value);
                }
                root.AddNode(groupNode);
            }

            __result = root;
            return false;
        }
        private static void Language_LoadLanguageFile_Postfix(ref string language, ref bool __result)
        {
            string js;
            Plugin.LogInfo("Trying to load " + language + " localization for mod.");
            foreach (ModInfo mod in Plugin.Mods)
            {
                if (File.Exists(mod.Directory + "\\Localization\\" + language + ".json"))
                {
                    Plugin.LogInfo("Done");
                    js = File.ReadAllText(mod.Directory + "\\Localization\\" + language + ".json");
                }
                else
                {
                    Plugin.LogInfo("Failed, loading English localization instead.");
                    js = File.ReadAllText(mod.Directory + "\\Localization\\English.json");
                }
                JsonData d = JsonMapper.ToObject(js);
                foreach (var v in d.Keys)
                {
                    languageStringsData.Add(v, d[v].ToString());
                }
            }
            __result = true;
        }
        private static bool Utils_TryParseEnumTechType_Prefix(ref string val, ref TechType result, ref bool __result)
        {
            foreach (ModInfo mod in Plugin.Mods)
            {
                foreach (Tech tech in mod.Tech.Values)
                {
                    if (val.ToLower() == tech.ID.ToLower())
                    {
                        result = tech.Type;
                        __result = true;
                        return false;
                    }
                }
            }
            try
            {
                result = (TechType)Enum.Parse(typeof(TechType), val, true);
            }
            catch
            {
                result = default;
                __result = false;
                return false;
            }
            __result = true;
            return false;
        }
        internal static void ApplyPatches(Harmony h)
        {
            h.Patch(typeof(KnownTech).GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(Tech).GetMethod("KnownTech_Initialize_Postfix", BindingFlags.NonPublic | BindingFlags.Static)));
            h.Patch(typeof(PrefabDatabase).GetMethod("LoadPrefabDatabase", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(Tech).GetMethod("PrefabDatabase_LoadPrefabDatabase_Postfix", BindingFlags.NonPublic | BindingFlags.Static)));
            h.Patch(typeof(CraftData).GetMethod("PreparePrefabIDCache", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(Tech).GetMethod("CraftData_PreparePrefabIDCache_Postfix", BindingFlags.NonPublic | BindingFlags.Static)));
            h.Patch(typeof(CraftTree).GetMethod("FabricatorScheme", BindingFlags.NonPublic | BindingFlags.Static), new HarmonyMethod(typeof(Tech).GetMethod("CraftTree_FabricatorScheme_Prefix", BindingFlags.NonPublic | BindingFlags.Static)));
            h.Patch(typeof(Language).GetMethod("LoadLanguageFile", BindingFlags.NonPublic | BindingFlags.Instance), null, new HarmonyMethod(typeof(Tech).GetMethod("Language_LoadLanguageFile_Postfix", BindingFlags.NonPublic | BindingFlags.Static)));
            h.Patch(typeof(UWE.Utils).GetMethod("TryParseEnum", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(typeof(TechType)), new HarmonyMethod(typeof(Tech).GetMethod("Utils_TryParseEnumTechType_Prefix", BindingFlags.NonPublic | BindingFlags.Static)));
        }

        public readonly TechType Type = Utils.GetNextEnumIndex<TechType>();

        public string ID;
        public CraftTree.Type CraftType = CraftTree.Type.None;

        public EquipmentType EquipmentSlot = EquipmentType.Hand;
        public QuickSlotType UseType = QuickSlotType.Selectable;
        public CraftData.BackgroundType Background = CraftData.BackgroundType.Normal;

        public string Group = "Resources";
        public string Category = "BasicMaterials";

        public float CraftingTime = 2f;
        public int CraftAmount = 1;

        public List<TechType> LinkedItems = new List<TechType>();
        public List<TechType> UnlockDependencies = new List<TechType>();

        public Vector2int InventorySize = new Vector2int(1, 1);

        public Dictionary<TechType, int> Recipe = new Dictionary<TechType, int>();

        public string PrefabID = "";
        public GameObject Prefab = null;

        public string IconName = "";
        public Atlas.Sprite Icon = SpriteManager.defaultSprite;

        public delegate void SetupDelegate();
        public SetupDelegate Setup;

        public Tech(string id)
        {
            ID = id;
        }
    }
    public class PickupableTech : Tech
    {
        private void SetupMethod()
        {
            if(Prefab == null)
            {
                Plugin.LogWarning("Prefab must not be null.");
                return;
            }
            if (!String.IsNullOrEmpty(PrefabID))
            {
                Plugin.LogWarning("Modifying builtin prefabs is not yet supported.");
                return;
            }
            if(!Prefab.GetComponent<Rigidbody>() || !Prefab.GetComponent<Collider>())
            {
                Plugin.LogWarning("Prefab must have Rigidbody and Collider in order to work properly.");
                return;
            }
            Prefab.AddComponent<TechTag>().type = Type;
            PrefabIdentifier pi = Prefab.AddComponent<PrefabIdentifier>();
            pi.Id = ID;
            pi.ClassId = ID;
            Prefab.AddComponent<ItemPrefabData>();
            Prefab.AddComponent<Pickupable>();
            Prefab.AddComponent<WorldForces>();
            Prefab.AddComponent<OnSurfaceTracker>();
            OnSurfaceDownforce osd = Prefab.AddComponent<OnSurfaceDownforce>();
            osd.onSurfaceTracker = Prefab.GetComponent<OnSurfaceTracker>();
            osd.useRigidbody = Prefab.GetComponent<Rigidbody>();
            osd.worldForces = Prefab.GetComponent<WorldForces>();
        }
        public PickupableTech(string id) : base(id)
        {
            Setup = SetupMethod;
        }
    }
}
