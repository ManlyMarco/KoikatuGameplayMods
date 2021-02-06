namespace KK_LewdCrestX
{
    internal static class Extensions
    {
        public static LewdCrestXController GetCrestController(this SaveData.Heroine heroine)
        {
            return GetCrestController(heroine?.chaCtrl);
        }

        public static LewdCrestXController GetCrestController(this ChaControl chaCtrl)
        {
            return chaCtrl != null ? chaCtrl.GetComponent<LewdCrestXController>() : null;
        }

        public static CrestType GetCurrentCrest(this SaveData.Heroine heroine)
        {
            return GetCurrentCrest(heroine?.chaCtrl);
        }

        public static CrestType GetCurrentCrest(this ChaControl chaCtrl)
        {
            var ctrl = GetCrestController(chaCtrl);
            return ctrl == null ? CrestType.None : ctrl.CurrentCrest;
        }

        /// <summary>
        /// Is current H mode penetration?
        /// </summary>
        public static bool IsSonyu(this HFlag hFlag)
        {
            return hFlag.mode == HFlag.EMode.sonyu || hFlag.mode == HFlag.EMode.sonyu3P || hFlag.mode == HFlag.EMode.sonyu3PMMF;
        }

        /// <summary>
        /// Is current h mode service?
        /// </summary>
        public static bool IsHoushi(this HFlag hFlag)
        {
            return hFlag.mode == HFlag.EMode.houshi || hFlag.mode == HFlag.EMode.houshi3P || hFlag.mode == HFlag.EMode.houshi3PMMF;
        }
    }
}