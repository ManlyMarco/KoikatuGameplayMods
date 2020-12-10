using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KK_TailWagger
{
    [BepInPlugin("TailWaggerPlugin", "TailWaggerPlugin", "1.0")]
    public class TailWaggerPlugin : BaseUnityPlugin
    {
        private static Harmony _hi;

        private void Awake()
        {
            _hi = Harmony.CreateAndPatchAll(typeof(TailWaggerPlugin));
        }

        private void OnDestroy()
        {
            _hi?.UnpatchAll(_hi.Id);
            _hi = null;
        }

        private static readonly Dictionary<ChaControl, HashSet<DynamicBone>> _targets =
            new Dictionary<ChaControl, HashSet<DynamicBone>>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryParent))]
        private static void AccChangeHook(ChaControl __instance, int slotNo)
        {
            if (_hi == null) return;
            Console.WriteLine("ChangeAccessoryParent slotNo=" + slotNo);

            var acc = AccessoriesApi.GetAccessory(__instance, slotNo);
            foreach (var b in acc.GetComponentsInChildren<DynamicBone>())
            {
                if (b.m_Root.name == "N_j_sippo_02")
                {
                    Console.WriteLine("found tail");

                    if (!_targets.TryGetValue(__instance, out var hs))
                    {
                        hs = new HashSet<DynamicBone>();
                        _targets[__instance] = hs;
                    }

                    hs.Add(b);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeMouthPtn))]
        private static void ExpressionChangeHook(ChaControl __instance, int ptn)
        {
            if (_hi == null) return;
            Console.WriteLine("ptn=" + ptn);

            if (!_targets.TryGetValue(__instance, out var targets)) return;

            foreach (var target in targets)
            {
                if (target == null) continue;

                target.m_Force = new Vector3(Random.value, Random.value, Random.value);
            }
        }

        private void Update()
        {
            foreach (var target in _targets)
            {
                if (target.Key == null) continue;
                foreach (var bone in target.Value)
                {
                    if (bone == null) continue;

                    bone.m_Force = Vector3.Lerp(new Vector3(0.01f, 0f, 0f), new Vector3(-0.01f, 0f, 0f), Mathf.SmoothStep(0, 1, Mathf.PingPong(Time.time, 0.5f) / 0.5f));
                }
            }
        }
    }
}
