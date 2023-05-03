using System;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using KKAPI;
using MoreShopItems.Features;
using UniRx;

namespace MoreShopItems
{
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, "More Shop Items", Version)]
    public class MoreShopItemsPlugin : BaseUnityPlugin
    {
        public const string GUID = "MoreShopItems";
        public const string Version = "2.1.2";

        internal const int DetectorItemId = 3456651;
        internal const int TalismanItemId = 3456650;

        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var cleanup = LoadFeatures();
#if DEBUG
            cleanup.AddTo(this);
#endif
        }

        /// <summary>
        /// Find all classes implementing IFeature and try to run their apply methods. If any fail to load, attempt to clean up after them.
        /// </summary>
        private IDisposable LoadFeatures()
        {
            var cleanupList = new CompositeDisposable();
            try
            {
                var loadedList = new StringBuilder("Loaded features: ");
                var interfaceT = typeof(IFeature);
                var ourAss = typeof(MoreShopItemsPlugin).Assembly;
                foreach (var featType in ourAss.GetTypes().Where(x => !x.IsAbstract && x.IsClass && interfaceT.IsAssignableFrom(x)))
                {
                    var disp = new CompositeDisposable();
                    try
                    {
                        var i = (IFeature)Activator.CreateInstance(featType);
                        if (i.ApplyFeature(ref disp, this))
                        {
                            cleanupList.Add(disp);
                            disp = new CompositeDisposable();
                            loadedList.Append(featType.Name);
                            loadedList.Append(" ");
                        }
                        else
                        {
                            disp.Clear();
                        }
                    }
                    catch (Exception e)
                    {
                        disp.Dispose();
                        disp.Clear();

                        Logger.LogWarning($"Failed to load feature {featType.FullName} with exception: " + e);
                    }
                }

                Logger.LogInfo(loadedList.ToString());

                return cleanupList;
            }
            catch
            {
                cleanupList.Dispose();
                throw;
            }
        }
    }
}
