using BepInEx.Configuration;
using HarmonyLib;

namespace KoikatuGameplayMod
{
    internal interface IFeature
    {
        bool Install(Harmony instance, ConfigFile config);
    }
}