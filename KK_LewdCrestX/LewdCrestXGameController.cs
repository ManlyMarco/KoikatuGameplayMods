using ActionGame;
using KKAPI.MainGame;
using UnityEngine;

namespace KK_LewdCrestX
{
    public sealed class LewdCrestXGameController : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            //todo use cached? might not detect all
            foreach (var controller in GameObject.FindObjectsOfType<LewdCrestXController>())
            {
                if (controller.CurrentCrest == CrestType.restore)
                {
                    var heroine = controller.Heroine;
                    if (heroine != null)
                    {
                        LewdCrestXPlugin.Logger.LogDebug("Resetting heroine to virgin because of restore crest: " + heroine.charFile.parameter.fullname);
                        heroine.isVirgin = true;
                        heroine.hCount = 0;
                    }
                }
            }
        }
    }
}