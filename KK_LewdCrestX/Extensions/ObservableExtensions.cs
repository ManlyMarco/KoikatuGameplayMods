using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static class ObservableExtensions
    {
        public static IObservable<Unit> OnGUIAsObservable(this Component component) => component == null ? Observable.Empty<Unit>() : component.GetOrAddComponent<ObservableOnGUITrigger>().OnGUIAsObservable();
        public static IObservable<Unit> OnGUIAsObservable(this Transform transform) => transform == null ? Observable.Empty<Unit>() : transform.GetOrAddComponent<ObservableOnGUITrigger>().OnGUIAsObservable();
        public static IObservable<Unit> OnGUIAsObservable(this GameObject gameObject) => gameObject == null ? Observable.Empty<Unit>() : gameObject.GetOrAddComponent<ObservableOnGUITrigger>().OnGUIAsObservable();
    }

    [DisallowMultipleComponent]
    internal class ObservableOnGUITrigger : ObservableTriggerBase
    {
        private void OnGUI()
        {
            _onGui?.OnNext(Unit.Default);
        }

        public IObservable<Unit> OnGUIAsObservable()
        {
            return _onGui ?? (_onGui = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            _onGui?.OnCompleted();
        }

        private Subject<Unit> _onGui;
    }
}