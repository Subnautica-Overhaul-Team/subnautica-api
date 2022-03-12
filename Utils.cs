using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace SubnauticaModloader
{
    static class Utils
    {
        private static readonly Dictionary<Type, int> indexes = new Dictionary<Type, int>();
        public static Atlas.Sprite LoadIcon(string filename)
        {
            if (File.Exists("Mods\\Resources\\" + filename))
            {
                byte[] data = File.ReadAllBytes("Mods\\Resources\\" + filename);
                Texture2D texture = new Texture2D(2, 2);
                ImageConversion.LoadImage(texture, data, false);
                texture.Apply();

                Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
                return new Atlas.Sprite(s, false);
            }
            return SpriteManager.defaultSprite;
        }
        internal static T GetNextEnumIndex<T>() where T: Enum
        {
            Array arr = Enum.GetValues(typeof(T));
            if (indexes.TryGetValue(typeof(T), out _))
            {
                indexes[typeof(T)]++;
            }
            else
            {
                indexes[typeof(T)] = 1;
            }
            return (T)Enum.ToObject(typeof(T), (int)arr.GetValue(arr.Length - 1) + indexes[typeof(T)]);
        }
    }
}
