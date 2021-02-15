using System;
using System.Reflection;
using ExtensibleSaveFormat;

namespace KK_Pregnancy
{
    public sealed class PregnancyData
    {
        public static readonly float DefaultFertility = 0.3f;

        /// <summary>
        /// Week at which pegnancy reaches max level and the character leaves school
        /// </summary>
        public static readonly int LeaveSchoolWeek = 41;

        /// <summary>
        /// Week at which pegracy ends and the character returns to school
        /// </summary>
        public static readonly int ReturnToSchoolWeek = LeaveSchoolWeek + 7;

        #region Names of these are important, used as dictionary keys

        /// <summary>
        /// The character is harder to get pregananant.
        /// </summary>
        public float Fertility = 0.3f;

        /// <summary>
        /// Should any gameplay code be executed for this character.
        /// If false the current pregancy week doesn't change and the character can't get pegnant.
        /// </summary>
        public bool GameplayEnabled = true;

        //TODO public MenstruationSchedule MenstruationSchedule = MenstruationSchedule.Default;

        /// <summary>
        /// If 0 or negative, the character is not pregant.
        /// If between 0 and <see cref="LeaveSchoolWeek"/> the character is pregant and the belly is proportionately sized.
        /// If equal or above <see cref="LeaveSchoolWeek"/> the character is on a maternal leave until <see cref="ReturnToSchoolWeek"/>.
        /// </summary>
        public int Week;

        /// <summary>
        /// How many times the character was pergant, including the current one.
        /// </summary>
        public int PregnancyCount;

        public int WeeksSinceLastPregnancy;

        /// <summary>
        /// Always have milk, even if not pergenant
        /// </summary>
        public bool AlwaysLactates;

        #endregion

        #region Save/Load

        private static readonly PregnancyData _default = new PregnancyData();
        private static readonly FieldInfo[] _serializedFields = typeof(PregnancyData).GetFields(BindingFlags.Public | BindingFlags.Instance);

        public static PregnancyData Load(PluginData data)
        {
            if (data?.data == null) return null;

            var result = new PregnancyData();
            foreach (var fieldInfo in _serializedFields)
            {
                if (data.data.TryGetValue(fieldInfo.Name, out var val))
                {
                    try
                    {
                        if (fieldInfo.FieldType.IsEnum) val = (int)val;
                        fieldInfo.SetValue(result, val);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }

            if (result.IsPregnant)
            {
                result.WeeksSinceLastPregnancy = 0;
                if (result.PregnancyCount == 0) result.PregnancyCount = 1;
            }

            return result;
        }

        public PluginData Save()
        {
            var result = new PluginData { version = 1 };
            foreach (var fieldInfo in _serializedFields)
            {
                var value = fieldInfo.GetValue(this);
                // Check if any value is different than default, if not then don't save any data
                var defaultValue = fieldInfo.GetValue(_default);
                if (!Equals(defaultValue, value))
                    result.data.Add(fieldInfo.Name, value);
            }

            return result.data.Count > 0 ? result : null;
        }

        #endregion

        // If week is 0 the character is not peregenent
        public bool IsPregnant => Week > 0;

        public void StartPregnancy()
        {
            if (GameplayEnabled && !IsPregnant)
            {
                Week = 1;
                PregnancyCount++;
                WeeksSinceLastPregnancy = 0;
            }
        }
    }
}