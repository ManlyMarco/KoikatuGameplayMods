using UniRx;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static class ObservableExtensions
    {
        public static IObservable<Unit> OnGUIAsObservable(this Component component) => component == null ? Observable.Empty<Unit>() : component.GetOrAddComponent<ObservableOnGUITrigger>().OnGUIAsObservable();
        public static IObservable<Unit> OnGUIAsObservable(this Transform transform) => transform == null ? Observable.Empty<Unit>() : transform.GetOrAddComponent<ObservableOnGUITrigger>().OnGUIAsObservable();
        public static IObservable<Unit> OnGUIAsObservable(this GameObject gameObject) => gameObject == null ? Observable.Empty<Unit>() : gameObject.GetOrAddComponent<ObservableOnGUITrigger>().OnGUIAsObservable();
    }
}