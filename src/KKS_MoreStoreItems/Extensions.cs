using System;
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
    }
}
