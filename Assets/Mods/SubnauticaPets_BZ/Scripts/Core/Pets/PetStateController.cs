using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    public enum PetState
    {
        Idle,
        Wandering,
        MovingTo,
        Sleeping,
        Dead
    }

    /// <summary>
    ///     Simple State Controller for pet actions
    /// </summary>
    internal class PetStateController : MonoBehaviour
    {
        [SerializeField] private PetState currState;
        private PetAction _currentAction;
        private IdleAction _idleAction;
        private KilledAction _killedAction;
        private MoveToAction _moveToAction;
        private SleepAction _sleepAction;

        private WanderAction _wanderAction;

        private void Awake()
        {
            _wanderAction = GetComponent<WanderAction>();
            _idleAction = GetComponent<IdleAction>();
            _moveToAction = GetComponent<MoveToAction>();
            _killedAction = GetComponent<KilledAction>();
            _sleepAction = GetComponent<SleepAction>();
        }

        private void Start()
        {
            _idleAction.Init();
            _wanderAction.Init();
            _moveToAction.Init();
            _killedAction.Init();
            _sleepAction.Init();

            _wanderAction.OnActionCompleted.AddListener(WanderActionComplete);
            _idleAction.OnActionCompleted.AddListener(IdleActionComplete);
            _moveToAction.OnActionCompleted.AddListener(MoveToActionComplete);
            _killedAction.OnActionCompleted.AddListener(KilledActionComplete);
            _sleepAction.OnActionCompleted.AddListener(SleepActionComplete);

            SetNewState(PetState.Idle);
        }

        private void Update()
        {
            CheckForForcedState();

            if (!_currentAction) return;
            _currentAction.UpdateAction();
        }

        internal void SetNewState(PetState newState)
        {
            if (currState == PetState.Dead) return;

            // ModDebugLog.LogDebug($"{gameObject.name}: Changing state from: {_currState} to {newState}");
            currState = newState;
            switch (newState)
            {
                case PetState.Idle:
                    SetNewAction(_idleAction);
                    break;
                case PetState.Sleeping:
                    SetNewAction(_sleepAction);
                    break;
                case PetState.Wandering:
                    SetNewAction(_wanderAction);
                    break;
                case PetState.MovingTo:
                    SetNewAction(_moveToAction);
                    break;
                case PetState.Dead:
                    SetNewAction(_killedAction);
                    break;
            }
        }

        /// <summary>
        ///     Called by Pet to move to player
        /// </summary>
        internal void MoveToPosition(Vector3 position)
        {
            _moveToAction.movePosition = position;
            SetNewState(PetState.MovingTo);
        }

        /// <summary>
        ///     Interrupts the current action and redirects the pet away from a Moonpool opening.
        /// </summary>
        internal void AvoidMoonpool(Vector3 safePosition)
        {
            if (currState == PetState.Dead) return;

            SetNewState(PetState.Wandering);
            _wanderAction.MoveToSafePosition(safePosition);
        }

        internal void Kill()
        {
            SetNewState(PetState.Dead);
        }

        private void WanderActionComplete()
        {
            SetNewState(PetState.Idle);
        }

        private void IdleActionComplete()
        {
            SetNewState(PetState.Wandering);
        }

        private void MoveToActionComplete()
        {
            SetNewState(PetState.Idle);
        }

        private void KilledActionComplete()
        {
            _currentAction = null;
        }

        private void SleepActionComplete()
        {
            SetNewState(PetState.Idle);
        }

        private void SetNewAction(PetAction newAction)
        {
            _currentAction?.EndAction();
            _currentAction = newAction;
            _currentAction.StartAction();
        }

        private void CheckForForcedState()
        {
            // Go to sleep
            if (currState != PetState.Sleeping && _sleepAction.ShouldBeSleeping()) SetNewState(PetState.Sleeping);
        }
    }
}
