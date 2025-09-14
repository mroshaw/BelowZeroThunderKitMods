using UnityEngine;
using Random = UnityEngine.Random;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    /// Simple action to kill pet
    /// </summary>
    internal class SleepAction : PetAction
    {

        [SerializeField] private float morningWakeUpTime = 0.15f;
        [SerializeField] private float eveningFallAsleepTime = 0.85f;
        [SerializeField] private float dayNightSleepRandomRange = 0.05f;
        
        private SimpleMovement _simpleMovement;
        private PetAnimator _petAnimator;
        
        private float _fallAsleepTime;
        private float _wakeUpTime;
        
        internal override void Init()
        {
            _simpleMovement = GetComponent<SimpleMovement>();
            _petAnimator = GetComponent<PetAnimator>();
            
            _wakeUpTime = morningWakeUpTime + Random.Range(-this.dayNightSleepRandomRange, this.dayNightSleepRandomRange);
            _fallAsleepTime = eveningFallAsleepTime + Random.Range(-this.dayNightSleepRandomRange, this.dayNightSleepRandomRange);
        }

        internal override void StartAction()
        {
            _simpleMovement.Stop();
            _petAnimator.SetSleeping(true);
        }

        internal override void EndAction()
        {
            _petAnimator.SetSleeping(false);
        }
        
        internal override void UpdateAction()
        {
            if (!ShouldBeSleeping())
            {
                ActionCompleted();
            }
        }

        internal bool ShouldBeSleeping()
        {
            float dayScalar = DayNightUtils.dayScalar;
            return dayScalar > _fallAsleepTime || dayScalar < _wakeUpTime;
        }
    }
}