using System;
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
        reduction,
        glow,
        gullible,
        lactation,
        liberated,
        libido,
        mantraction,
        messaging,
        mindmelt,
        pheromone,
        receptacle,
        regrowth,
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
        broodmother
    }

    public partial class LewdCrestXPlugin
    {
        internal static readonly HashSet<CrestType> ImplementedCrestTypes = new HashSet<CrestType>
        {
            CrestType.breedgasm,
            CrestType.command,
            CrestType.lactation,
            CrestType.liberated,
            CrestType.libido,
            CrestType.mantraction,
            CrestType.mindmelt,
            CrestType.regrowth,
            CrestType.restore,
            CrestType.siphoning,
            CrestType.suffer,
            CrestType.triggered,
            CrestType.vibrancy,
            CrestType.violove,
            CrestType.broodmother,
            CrestType.reduction
        };
    }
}