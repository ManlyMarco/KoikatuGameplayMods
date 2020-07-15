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
        private readonly PregnancyBoneEffect _boneEffect;
        private PregnancyData _pregnancyData;

        public PregnancyCharaController()
        {
            _pregnancyData = new PregnancyData();
            _boneEffect = new PregnancyBoneEffect(this);
        }

        /// <summary>
        /// The character is harder to get pregnant.
        /// </summary>
        public float Fertility
        {
            get => _pregnancyData.Fertility;
            set => _pregnancyData.Fertility = value;
        }

        /// <summary>
        /// Should any gameplay code be executed for this character.
        /// If false the current pregnancy week doesn't change and the character can't get pregnant.
        /// </summary>
        public bool GameplayEnabled
        {
            get => _pregnancyData.GameplayEnabled;
            set => _pregnancyData.GameplayEnabled = value;
        }

        /// <summary>
        /// If 0 or negative, the character is not pregnant.
        /// If between 0 and <see cref="PregnancyData.LeaveSchoolWeek"/> the character is pregnant and the belly is proportionately sized.
        /// If equal or above <see cref="PregnancyData.LeaveSchoolWeek"/> the character is on a maternal leave until <see cref="PregnancyData.ReturnToSchoolWeek"/>.
        /// </summary>
        public int Week
        {
            get => _pregnancyData.Week;
            set => _pregnancyData.Week = value;
        }

        public MenstruationSchedule Schedule
        {
            get => _pregnancyData.MenstruationSchedule;
            set => _pregnancyData.MenstruationSchedule = value;
        }

        public float GetBellySizePercent()
        {
            // Don't show any effect at week 1 since it begins right after winning a child lottery
            return Mathf.Clamp01((Week - 1f) / (PregnancyData.LeaveSchoolWeek - 1f));
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
            SetExtendedData(_pregnancyData.Save());
        }

        public void ReadData()
        {
            var data = GetExtendedData();
            _pregnancyData = PregnancyData.Load(data) ?? new PregnancyData();

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
            // Parameters are false by default in class chara maker, so we need to load them the 1st time to not lose progress
            // !MakerAPI.InsideAndLoaded is true when the initial card is being loaded into maker so we can use that
            if (!MakerAPI.InsideAndLoaded || MakerAPI.GetCharacterLoadFlags()?.Parameters != false)
            {
                ReadData();

                GetComponent<BoneController>().AddBoneEffect(_boneEffect);
            }
        }

        internal static byte[] GetMenstruationsArr(MenstruationSchedule menstruationSchedule)
        {
            switch (menstruationSchedule)
            {
                default:
                    return HFlag.menstruations;
                case MenstruationSchedule.MostlyRisky:
                    return _menstruationsRisky;
                case MenstruationSchedule.AlwaysSafe:
                    return _menstruationsAlwaysSafe;
                case MenstruationSchedule.AlwaysRisky:
                    return _menstruationsAlwaysRisky;
            }
        }

        private static readonly byte[] _menstruationsRisky = {
            0,
            0,
            0,
            0,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            0,
            0
        };

        // Always needs at least one day of different type to prevent infinite loop when trying to set that type of day
        private static readonly byte[] _menstruationsAlwaysSafe = {
            0,
            0,
            0,
            0,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        };

        // Always needs at least one day of different type to prevent infinite loop when trying to set that type of day
        private static readonly byte[] _menstruationsAlwaysRisky = {
            0,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1
        };
    }
}