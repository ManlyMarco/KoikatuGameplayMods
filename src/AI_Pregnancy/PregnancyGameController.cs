using System;
using System.Collections.Generic;
using ExtensibleSaveFormat;
using KKAPI.Chara;
using KKAPI.MainGame;
using UnityEngine;
using Random = UnityEngine.Random;
using AIProject;
using AIProject.SaveData;

namespace KK_Pregnancy
{
    public class PregnancyGameController : GameCustomFunctionController
    {
        private static readonly HashSet<AgentData> _startedPregnancies = new HashSet<AgentData>();

        internal static bool InsideHScene { get; private set; }

        protected override void OnDayChange(int day)
        {
            // 1 day = 1 week in AI
            // At start of each week increase pregnancy week counters of all pregnant characters
            ApplyToAllDatas((heroine, data) => AddPregnancyWeek(data));
            SetCharsDangerousDay(day);
        }

        //In AI we set Dangerous day by 20% chance on any new day
        private void SetCharsDangerousDay(int day)
        {
            var handlers = CharacterApi.GetRegisteredBehaviour(PregnancyPlugin.GUID);
            var random = new System.Random();

            foreach (PregnancyCharaController charCustFunCtrl in handlers.Instances)
            {
                //Check always or never risky settings
                if (charCustFunCtrl.Data.MenstruationSchedule == MenstruationSchedule.AlwaysSafe)
                {
                    charCustFunCtrl.Data.StopMenstration();
                    charCustFunCtrl.isDangerousDay = false;
                    charCustFunCtrl.SetExtendedData(charCustFunCtrl.Data.Save());
                    continue;
                }
                if (charCustFunCtrl.Data.MenstruationSchedule == MenstruationSchedule.AlwaysRisky)
                {
                    charCustFunCtrl.Data.StartMenstration(day);
                    charCustFunCtrl.isDangerousDay = true;
                    charCustFunCtrl.SetExtendedData(charCustFunCtrl.Data.Save());
                    continue;
                }

                //If already menstrating then check if it should end
                if (charCustFunCtrl.Data.MenstrationStartDay > -1)
                {
                    var daysSinceStart = day - charCustFunCtrl.Data.MenstrationStartDay;
                    var totalDaysAllowed = charCustFunCtrl.Data.MenstruationSchedule == MenstruationSchedule.Default ? 2 : 3;

                    //Stop menstration when x days passed based on the menstration schedule
                    if (daysSinceStart >= totalDaysAllowed)
                    {
                        charCustFunCtrl.Data.StopMenstration();
                        charCustFunCtrl.isDangerousDay = false;
                        charCustFunCtrl.SetExtendedData(charCustFunCtrl.Data.Save());
                    }

                    continue;
                }

                //Set each characters risky day randomly
                if (random.Next(0, 100) <= 20)
                {
                    charCustFunCtrl.Data.StartMenstration(day);
                    charCustFunCtrl.isDangerousDay = true;
                    charCustFunCtrl.SetExtendedData(charCustFunCtrl.Data.Save());
                    // PregnancyPlugin.Logger.LogDebug($"Preg - StartMenstration {charCustFunCtrl.isDangerousDay}  {charCustFunCtrl.ChaControl.name}");
                }
            }
        }

        protected override void OnStartH(HScene proc, bool freeH)
        {
            InsideHScene = true;
            // proc.gameObject.AddComponent<LactationController>(); //Add later  
        }

        protected override void OnEndH(HScene proc, bool freeH)
        {
            InsideHScene = false;
            // Destroy(proc.GetComponent<LactationController>()); //Add later

            // Figure out if conception happened at end of h scene
            var heroine = Manager.HSceneManager.Instance?.females[0];//In AI Agent actor list does not contain merchant, use females list instead
            if (heroine == null) return;

            var controller = heroine.ChaControl.GetComponent<PregnancyCharaController>();
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            //In AI see if the current day is a risky day
            if (!controller.isDangerousDay) return;

            var cameInside = PregnancyPlugin.ConceptionEnabled.Value && proc.ctrlFlag.numInside > 0;
            var cameInsideAnal = PregnancyPlugin.AnalConceptionEnabled.Value && proc.ctrlFlag.numAnal > 0;
            if (cameInside || cameInsideAnal)
            {
                //Allow pregnancy if enabled, or overridden, and is not currently pregnant
                if (!controller.Data.GameplayEnabled || controller.Data.IsPregnant) return;

                var fertility = Mathf.Max(PregnancyPlugin.FertilityOverride.Value, controller.Data.Fertility);

                var winThreshold = Mathf.RoundToInt(fertility * 100);
                var childLottery = Random.Range(1, 100);
                //PregnancyPlugin.Logger.LogDebug($"Preg - OnEndH calc pregnancy chance {childLottery} to {winThreshold}");
                var wonAChild = winThreshold >= childLottery;
                if (wonAChild)
                {
                    // PregnancyPlugin.Logger.LogDebug("Preg - child lottery won, pregnancy will start");                        
                    //In AI we have to immediately set the preg state, or we lose it if the user saves and exits before PeriodChange
                    _startedPregnancies.Add(heroine.ChaControl.GetHeroine());
                    ProcessPendingChanges();
                    //Keep charaCtrl's copy in sync
                    controller.Data.StartPregnancy();
                }
            }
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            _startedPregnancies.Clear();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            ProcessPendingChanges();
        }

        protected override void OnPeriodChange(AIProject.TimeZone period)
        {
            ProcessPendingChanges();
        }

        private static void ProcessPendingChanges()
        {
            ApplyToAllDatas((chara, data) =>
            {
                if (chara is AgentData heroine && _startedPregnancies.Contains(chara) && !data.IsPregnant)
                {
                    data.StartPregnancy();
                    return true;
                }
                return false;
            });
            _startedPregnancies.Clear();
        }

        private static void ApplyToAllDatas(Func<AgentData, PregnancyData, bool> action)
        {

            void ApplyToDatas(AgentData character)
            {
                var chafiles = character.GetRelatedChaFiles();
                if (chafiles == null) return;

                foreach (var chaFile in chafiles)
                {
                    var data = ExtendedSave.GetExtendedDataById(chaFile, PregnancyPlugin.GUID);
                    var pd = PregnancyData.Load(data) ?? new PregnancyData();
                    if (action(character, pd))
                        ExtendedSave.SetExtendedDataById(chaFile, PregnancyPlugin.GUID, pd.Save());
                }
            }

            var heroineList = GetHeroineList();
            if (heroineList == null) return;

            foreach (var heroine in heroineList)
            {
                ApplyToDatas(heroine);
            }
            // ApplyToDatas(Singleton<Map>.Instance.Player.AgentPartner.AgentData);  TODO find male AgentData, if we want to match what KK is now doing

            // If controller exists then update its state so it gets any pregnancy week updates
            foreach (var controller in FindObjectsOfType<PregnancyCharaController>())
                controller.ReadData();
        }

        private static List<AgentData> GetHeroineList()
        {
            var heroineList = new List<AgentData>();
            var agentTable = Manager.Map.Instance.AgentTable;
            if (agentTable == null || agentTable.Count <= 0) return null;

            foreach (int key in agentTable.Keys)
            {
                if (agentTable.TryGetValue(key, out AgentActor agentActor))
                {
                    heroineList.Add(agentActor.AgentData);
                }
            }

            return heroineList;
        }

        private static bool AddPregnancyWeek(PregnancyData pd)
        {
            if (pd == null || !pd.GameplayEnabled) return false;

            if (pd.IsPregnant)
            {
                if (pd.Week < PregnancyData.LeaveSchoolWeek)
                {
                    // Advance through pregnancy at full configured speed
                    var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                    pd.Week = Mathf.Min(PregnancyData.LeaveSchoolWeek, pd.Week + weekChange);
                }
                else if (pd.Week >= PregnancyData.LeaveSchoolWeek)
                {
                    pd.Week = 0;
                }
                // PregnancyPlugin.Logger.LogDebug($"Preg - pregnancy week is now {pd.Week}");
            }
            else if (pd.PregnancyCount > 0)
            {
                pd.WeeksSinceLastPregnancy++;
            }

            return true;
        }
    }
}
