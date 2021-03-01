using System;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
#if KK
    using ActionGame;    
#endif
using KKAPI.MainGame;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;
#if AI
    using AIChara;
    using AIProject;
    using AIProject.SaveData;
#endif

namespace KK_Pregnancy
{
    public class PregnancyGameController : GameCustomFunctionController
    {
        #if KK
            private static readonly HashSet<SaveData.Heroine> _startedPregnancies = new HashSet<SaveData.Heroine>();
        #elif AI
            private static readonly HashSet<AgentData> _startedPregnancies = new HashSet<AgentData>();
        #endif

        internal static bool InsideHScene { get; private set; }


        #if KK
        
            protected override void OnDayChange(Cycle.Week day)
            {
                // Use Sunday for weekly stuff because it is always triggered (all other days can get skipped)
                if (day == Cycle.Week.Holiday)
                {
                    // At start of each week increase pregnancy week counters of all pregnant characters
                    ApplyToAllDatas((chara, data) => AddPregnancyWeek(data));
                }                                
            }

        #elif AI

            protected override void OnDayChange(int day)
            {
                // 1 day = 1 week in AI
                // At start of each week increase pregnancy week counters of all pregnant characters
                ApplyToAllDatas((heroine, data) => AddPregnancyWeek(data));
            }

        #endif        

        #if KK
            protected override void OnStartH(HSceneProc proc, bool freeH)
            {
                InsideHScene = true;
                proc.gameObject.AddComponent<LactationController>();
            }
        
        #elif AI

            protected override void OnStartH(HScene proc, bool freeH)
            {
                InsideHScene = true;
                // proc.gameObject.AddComponent<LactationController>(); //TODO  
            }

        #endif


        #if KK
            protected override void OnEndH(HSceneProc proc, bool freeH)
            {
                InsideHScene = false;
                Destroy(proc.GetComponent<LactationController>());

                // Figure out if conception happened at end of h scene
                // bug Don't know which character is which
                if (proc.flags.mode == HFlag.EMode.houshi3P || proc.flags.mode == HFlag.EMode.sonyu3P) return;

                var heroine = proc. flags.lstHeroine.First(x => x != null);
                var isDangerousDay = HFlag.GetMenstruation(heroine.MenstruationDay) == HFlag.MenstruationType.危険日;
                if (!isDangerousDay) return;

                var cameInside = PregnancyPlugin.ConceptionEnabled.Value && proc.flags.count.sonyuInside > 0;
                var cameInsideAnal = PregnancyPlugin.AnalConceptionEnabled.Value && proc.flags.count.sonyuAnalInside > 0;
                if (cameInside || cameInsideAnal)
                {
                    var controller = heroine.chaCtrl.GetComponent<PregnancyCharaController>();
                    if (controller == null) throw new ArgumentNullException(nameof(controller));

                    //Allow pregnancy if enabled, or overridden, and is not currently pregnant
                    if (!controller.Data.GameplayEnabled || controller.Data.IsPregnant) return;

                    var fertility = Mathf.Max(PregnancyPlugin.FertilityOverride.Value, controller.Data.Fertility);

                    var winThreshold = Mathf.RoundToInt(fertility * 100);
                    var childLottery = Random.Range(1, 100);
                    //Logger.Log(LogLevel.Debug, $"Preg - OnEndH calc pregnancy chance {childLottery} to {winThreshold}");
                    var wonAChild = winThreshold >= childLottery;
                    if (wonAChild)
                    {
                        //Logger.Log(LogLevel.Debug, "Preg - child lottery won, pregnancy will start");
                        _startedPregnancies.Add(heroine);
                    }
                }
            }

        #elif AI

            protected override void OnEndH(HScene proc, bool freeH)
            {
                InsideHScene = false;
                // Destroy(proc.GetComponent<LactationController>()); //TODO

                // Figure out if conception happened at end of h scene
                var heroine = Manager.HSceneManager.Instance?.Agent[0]?.AgentData;
                if (heroine == null) return;

                var cameInside = PregnancyPlugin.ConceptionEnabled.Value && proc.ctrlFlag.numInside > 0;
                var cameInsideAnal = PregnancyPlugin.AnalConceptionEnabled.Value && proc.ctrlFlag.numAnal > 0;
                if (cameInside || cameInsideAnal)
                {
                    var controller = heroine.GetNPC().ChaControl.GetComponent<PregnancyCharaController>();
                    if (controller == null) throw new ArgumentNullException(nameof(controller));

                    //Allow pregnancy if enabled, or overridden, and is not currently pregnant
                    if (!controller.Data.GameplayEnabled || controller.Data.IsPregnant) return;

                    var fertility = Mathf.Max(PregnancyPlugin.FertilityOverride.Value, controller.Data.Fertility);

                    var winThreshold = Mathf.RoundToInt(fertility * 100);
                    var childLottery = Random.Range(1, 100);
                    //Logger.Log(LogLevel.Debug, $"Preg - OnEndH calc pregnancy chance {childLottery} to {winThreshold}");
                    var wonAChild = winThreshold >= childLottery;
                    if (wonAChild)
                    {
                        PregnancyPlugin.Logger.LogDebug("Preg - child lottery won, pregnancy will start");
                        _startedPregnancies.Add(heroine);
                    }
                }
            }

        #endif
        
        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            _startedPregnancies.Clear();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            ProcessPendingChanges();
        }

        #if KK

            protected override void OnPeriodChange(Cycle.Type period)
            {
                ProcessPendingChanges();
            }

            private static void ProcessPendingChanges()
            {
                ApplyToAllDatas((chara, data) =>
                {
                    if (chara is SaveData.Heroine heroine && _startedPregnancies.Contains(heroine) && !data.IsPregnant)
                    {
                        data.StartPregnancy();
                        return true;
                    }
                    return false;
                });
                _startedPregnancies.Clear();
            }

        #elif AI

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
            
        #endif


        #if KK
            private static void ApplyToAllDatas(Func<SaveData.CharaData, PregnancyData, bool> action)
            {
                void ApplyToDatas(SaveData.CharaData character)
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

                foreach (var heroine in Game.Instance.HeroineList) ApplyToDatas(heroine);
                ApplyToDatas(Game.Instance.Player);

                // If controller exists then update its state so it gets any pregnancy week updates
                foreach (var controller in FindObjectsOfType<PregnancyCharaController>())
                    controller.ReadData();
            }

        #elif AI

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
                // ApplyToDatas(Singleton<Map>.Instance.Player.AgentPartner.AgentData);  TODO find male AgentData

                // If controller exists then update its state so it gets any pregnancy week updates
                foreach (var controller in FindObjectsOfType<PregnancyCharaController>())
                    controller.ReadData();
            }

            //TODO should add to AIAPI
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
        #endif



        private static bool AddPregnancyWeek(PregnancyData pd)
        {
            if (pd == null || !pd.GameplayEnabled) return false;

            if (pd.IsPregnant)
            {
                #if KK
                    if (pd.Week < PregnancyData.LeaveSchoolWeek)
                    {
                        // Advance through in-school at full configured speed
                        var weekChange = PregnancyPlugin.PregnancyProgressionSpeed.Value;
                        pd.Week = Mathf.Min(PregnancyData.LeaveSchoolWeek, pd.Week + weekChange);
                    }
                    else if (pd.Week < PregnancyData.ReturnToSchoolWeek)
                    {
                        // Make sure at least one week is spent out of school
                        var weekChange = Mathf.Min(PregnancyData.ReturnToSchoolWeek - PregnancyData.LeaveSchoolWeek - 1, PregnancyPlugin.PregnancyProgressionSpeed.Value);
                        pd.Week = pd.Week + weekChange;
                    }

                    if (pd.Week >= PregnancyData.ReturnToSchoolWeek)
                        pd.Week = 0;

                #elif AI

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

                #endif

                PregnancyPlugin.Logger.LogDebug($"Preg - pregnancy week is now {pd.Week}");
            }
            else if (pd.PregnancyCount > 0)
            {
                pd.WeeksSinceLastPregnancy++;
            }

            return true;
        }
    }
}
