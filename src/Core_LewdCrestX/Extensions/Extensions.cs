using System;
using ExtensibleSaveFormat;
using HarmonyLib;

namespace KK_LewdCrestX
{
    internal static class Extensions
    {
        public static LewdCrestXController GetCrestController(this SaveData.Heroine heroine)
        {
            return GetCrestController(heroine?.chaCtrl);
        }

        public static LewdCrestXController GetCrestController(this ChaControl chaCtrl)
        {
            return chaCtrl != null ? chaCtrl.GetComponent<LewdCrestXController>() : null;
        }

        public static CrestType GetCurrentCrest(this SaveData.Heroine heroine)
        {
            return GetCurrentCrest(heroine?.chaCtrl);
        }

        public static CrestType GetCurrentCrest(this ChaControl chaCtrl)
        {
            var ctrl = GetCrestController(chaCtrl);
            return ctrl == null ? CrestType.None : ctrl.CurrentCrest;
        }

        public static void ReadFromData<T>(this object settingContainer, PluginData data, string propName, T defaultValue)
        {
            try
            {
                var s = AccessTools.PropertySetter(typeof(LewdCrestXController), propName);
                if (data != null)
                {
                    if (data.data.TryGetValue(propName, out var value))
                    {
                        if (typeof(T).IsEnum) value = (int)value;
                        s.Invoke(settingContainer, new object[] { value });
                        return;
                    }
                }
                s.Invoke(settingContainer, new object[] { defaultValue });
            }
            catch (Exception ex)
            {
                LewdCrestXPlugin.Logger.LogError(ex);
            }
        }

        public static void SaveToData<T>(this object settingContainer, PluginData data, string propName, T defaultValue)
        {
            var value = AccessTools.PropertyGetter(typeof(LewdCrestXController), propName).Invoke(settingContainer, new object[0]);
            // Check if any value is different than default, if not then don't save any data
            if (!Equals(defaultValue, value))
                data.data.Add(propName, value);
        }

        public static string GetFullname(this SaveData.CharaData character)
        {
            return character.charFile?.parameter?.fullname ?? "???";
        }
    }
}