using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace KK_LewdCrestX
{
    [DisallowMultipleComponent]
    public class ObservableOnGUITrigger : ObservableTriggerBase
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