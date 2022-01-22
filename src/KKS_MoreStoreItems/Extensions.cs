using System;
using HarmonyLib;
using UniRx;

namespace MoreShopItems
{
    internal static class Extensions
    {
        public static void Add(this CompositeDisposable disp, UnityEngine.Object obj)
        {
            if (disp == null) throw new ArgumentNullException(nameof(disp));
            disp.Add(Disposable.Create(() => UnityEngine.Object.Destroy(obj)));
        }
        public static void Add(this CompositeDisposable disp, Harmony hi)
        {
            if (disp == null) throw new ArgumentNullException(nameof(disp));
            disp.Add(Disposable.Create(() => hi?.UnpatchSelf()));
        }
    }
}
