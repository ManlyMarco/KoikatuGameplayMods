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
        public PregnancyData Data { get; private set; }

        public PregnancyCharaController()
        {
            Data = new PregnancyData();
            _boneEffect = new PregnancyBoneEffect(this);
        }

        /// <summary>
        /// 0-1
        /// </summary>
        public float GetPregnancyEffectPercent()
        {
            // Don't show any effect at week 1 since it begins right after winning a child lottery
            return Mathf.Clamp01((Data.Week - 1f) / (PregnancyData.LeaveSchoolWeek - 1f));
        }

        public bool CanGetDangerousDays()
        {
            return Data.Week <= 1;
        }

        public void SaveData()
        {
            SetExtendedData(Data.Save());
        }

        public void ReadData()
        {
            var data = GetExtendedData();
            Data = PregnancyData.Load(data) ?? new PregnancyData();

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

        #region Inflation

        public static readonly int MaxInflationAmount = 15;

        private float _inflationChange;
        private int _inflationAmount;

        public int InflationAmount
        {
            get => _inflationAmount;
            set => _inflationAmount = Mathf.Clamp(value, 0, MaxInflationAmount);
        }

        public bool IsInflated => _inflationAmount > 0;

        /// <summary>
        /// 0-1
        /// </summary>
        public float GetInflationEffectPercent()
        {
            // Don't show any effect at first since there's still space
            return Mathf.Clamp01((InflationAmount + _inflationChange - 1f) / (MaxInflationAmount - 1f));
        }

        public void AddInflation(int amount)
        {
            var orig = InflationAmount;
            InflationAmount += amount;
            var change = InflationAmount - orig;
            _inflationChange -= change;
        }

        public void DrainInflation(int amount)
        {
            var orig = InflationAmount;
            InflationAmount -= amount;
            var change = orig - InflationAmount;
            _inflationChange += change;
        }

        protected override void Update()
        {
            base.Update();

            if (_inflationChange > 0)
                _inflationChange = Mathf.Max(0, _inflationChange - Time.deltaTime / 2);
            else if (_inflationChange < 0)
                _inflationChange = Mathf.Min(0, _inflationChange + Time.deltaTime / 2);
        }

        #endregion
    }
}