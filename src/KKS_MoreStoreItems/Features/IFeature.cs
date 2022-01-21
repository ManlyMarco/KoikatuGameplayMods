using UniRx;

namespace MoreShopItems.Features
{
    public interface IFeature
    {
        bool ApplyFeature(ref CompositeDisposable disp, MoreShopItemsPlugin inst);
    }
}