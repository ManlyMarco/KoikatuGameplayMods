using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;

namespace KoikatuGameplayMod
{
    [BepInPlugin("KoikatuGameplayMod", "Koikatu Gameplay Tweaks and Improvements", "1.0")]
    [BepInDependency("marco-gameplaymod", BepInDependency.DependencyFlags.SoftDependency)]
    public class KoikatuGameplayMod : BaseUnityPlugin
    {
        public KoikatuGameplayMod()
        {
            Hooks.ApplyHooks();
        }

        /*private void Update()
        {

        }

        private void Start()
        {
        }

        private void OnGUI()
        {

        }*/
    }
}
