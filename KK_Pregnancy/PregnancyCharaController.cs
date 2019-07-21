using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using UnityEngine;

namespace KK_Pregnancy
{
    public class PregnancyCharaController : CharaCustomFunctionController
    {
        private PregnancyBoneEffect _boneEffect;
        private float _fertility;
        private bool _gameplayEnabled;
        private int _week;

        /// <summary>
        /// The character is harder to get pregnant.
        /// </summary>
        public float Fertility
        {
            get => _fertility;
            set => _fertility = value;
        }

        /// <summary>
        /// Should any gameplay code be executed for this character.
        /// If false the current pregnancy week doesn't change and the character can't get pregnant.
        /// </summary>
        public bool GameplayEnabled
        {
            get => _gameplayEnabled;
            set => _gameplayEnabled = value;
        }

        /// <summary>
        /// If 0 or negative, the character is not pregnant.
        /// If between 0 and <see cref="PregnancyDataUtils.LeaveSchoolWeek"/> the character is pregnant and the belly is proportionately sized.
        /// If equal or above <see cref="PregnancyDataUtils.LeaveSchoolWeek"/> the character is on a maternal leave until <see cref="PregnancyDataUtils.ReturnToSchoolWeek"/>.
        /// </summary>
        public int Week
        {
            get => _week;
            set => _week = value;
        }

        public float GetBellySizePercent()
        {
            // Don't show any effect at week 1 since it begins right after winning a child lottery
            return Mathf.Clamp01((Week - 1f) / (PregnancyDataUtils.LeaveSchoolWeek - 1f));
        }

        public bool IsDuringPregnancy()
        {
            return Week > 0;
        }

        public bool CanGetDangerousDays()
        {
            return Week <= 1;
        }

        public void SaveData()
        {
            SetExtendedData(PregnancyDataUtils.SerializeData(Week, GameplayEnabled, Fertility));
        }

        public void ReadData()
        {
            var data = GetExtendedData();
            PregnancyDataUtils.DeserializeData(data, out _week, out _gameplayEnabled, out _fertility);

            if (!CanGetDangerousDays())
            {
                // Force the girl to always be on the safe day, happens every day after day of conception
                var heroine = ChaControl.GetHeroine();
                if (heroine != null)
                    HFlag.SetMenstruation(heroine, HFlag.MenstruationType.安全日);
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            SaveData();
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            if (MakerAPI.GetCharacterLoadFlags()?.Parameters == false) return;

            ReadData();

            if (_boneEffect == null)
                _boneEffect = new PregnancyBoneEffect(this);

            GetComponent<BoneController>().AddBoneEffect(_boneEffect);

            PregnancyGui.UpdateMakerInterface(this);
        }
    }
}