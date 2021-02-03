using ActionGame;
using KKAPI.MainGame;

namespace KK_LewdCrestX
{
    public sealed class LewdCrestXGameController : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            foreach (var heroine in Manager.Game.Instance.HeroineList)
            {
                if (heroine != null && heroine.GetCurrentCrest() == CrestType.restore && !heroine.isVirgin)
                {
                    LewdCrestXPlugin.Logger.LogDebug("Resetting heroine to virgin because of restore crest: " + heroine.charFile?.parameter?.fullname);
                    heroine.isVirgin = true;
                    heroine.hCount = 0;
                }
            }
        }
    }
}