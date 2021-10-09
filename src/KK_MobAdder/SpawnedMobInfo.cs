using UnityEngine;

namespace KK_MobAdder
{
    internal sealed class SpawnedMobInfo
    {
        public readonly GameObject Object;
        public readonly Vector3 InitialPosition;
        public readonly Quaternion InitialRotation;

        public void ResetPosAndRot()
        {
            Object.transform.SetPositionAndRotation(InitialPosition, InitialRotation);
        }

        public SpawnedMobInfo(GameObject o, Vector3 initialPosition, Quaternion initialRotation)
        {
            Object = o;
            InitialPosition = initialPosition;
            InitialRotation = initialRotation;
        }
    }
}