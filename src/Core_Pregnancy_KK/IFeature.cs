using BepInEx.Configuration;
using HarmonyLib;

namespace KK_Pregnancy
{
    internal interface IFeature
    {
        bool Install(Harmony instance, ConfigFile config);
    }
}