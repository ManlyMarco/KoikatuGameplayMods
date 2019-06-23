using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;

namespace KK_Pregnancy
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KKABMX.Core.KKABMX_Core.GUID)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    public class PregnancyPlugin : BaseUnityPlugin
    {
        public const string GUID = "KK_Pregnancy";
        internal const string Version = "0.1";

        private void Awake()
        {
            throw new System.NotImplementedException();
        }

        private void Start()
        {
            throw new System.NotImplementedException();
        }

        private void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}
