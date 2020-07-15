using System;
using System.Reflection;
using ExtensibleSaveFormat;

namespace KK_Pregnancy
{
    public sealed class PregnancyData
    {
        public static readonly float DefaultFertility = 0.3f;

        /// <summary>
        /// Week at which pregnancy reaches max level and the girl leaves school
        /// </summary>
        public static readonly int LeaveSchoolWeek = 41;

        /// <summary>
        /// Week at which pregnancy ends and the girl returns to school
        /// </summary>
        public static readonly int ReturnToSchoolWeek = LeaveSchoolWeek + 7;

        #region Names of these are important, used as dictionary keys
        public float Fertility;
        public bool GameplayEnabled;
        public MenstruationSchedule MenstruationSchedule;
        public int Week;
        #endregion

        public static PregnancyData Load(PluginData data)
        {
            if (data?.data == null) return null;

            var result = new PregnancyData();
            foreach (var fieldInfo in typeof(PregnancyData).GetFields(BindingFlags.Public | BindingFlags.Instance))
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

            return result;
        }

        public PluginData Save()
        {
            var result = new PluginData { version = 1 };
            foreach (var fieldInfo in typeof(PregnancyData).GetFields())
            {
                var value = fieldInfo.GetValue(this);
                // Check if any value is different than default, if not then don't save any data
                var defaultValue = fieldInfo.FieldType.IsValueType ? Activator.CreateInstance(fieldInfo.FieldType) : null;
                if (!Equals(defaultValue, value))
                    result.data.Add(fieldInfo.Name, value);
            }

            return result.data.Count > 0 ? result : null;
        }

        // If week is 0 the character is not pregnant
        public bool IsPregnant => Week > 0;

        public void StartPregnancy()
        {
            if (GameplayEnabled && !IsPregnant)
                Week = 1;
        }
    }
}