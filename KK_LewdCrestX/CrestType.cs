using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KK_LewdCrestX
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CrestType
    {
        None = 0,

        animalistic,
        breedgasm,
        command,
        cumdiction,
        deepfocus,
        destruction,
        forlorn,
        gaze,
        glow,
        inferiority,
        lactation,
        lesser,
        liberated,
        libido,
        mantraction,
        messaging,
        mindmelt,
        pheromone,
        receptacle,
        reprogram,
        restore,
        sensitivity,
        siphoning,
        slave,
        soulchained,
        statistics,
        suffer,
        task,
        triggered,
        vibrancy,
        violove,
        wombgasm
    }

    public partial class LewdCrestXPlugin
    {
        internal static readonly HashSet<CrestType> ImplementedCrestTypes = new HashSet<CrestType>{
            CrestType.command,
            CrestType.lactation,
            CrestType.liberated,
            CrestType.libido,
            CrestType.mindmelt,
            CrestType.restore,
            CrestType.siphoning,
            CrestType.vibrancy,
        };
    }
}