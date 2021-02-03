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
    }
}