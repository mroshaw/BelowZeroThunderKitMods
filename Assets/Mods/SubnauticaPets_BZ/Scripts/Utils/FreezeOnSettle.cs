using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DaftAppleGames.SubnauticaPets.Utils
{
    public enum FreezeCheckType { Velocity, GroundCheck, Both, Either }

    /// <summary>
    /// Locks RigidBody constraints once the object has settled
    /// </summary>
    internal class FreezeOnSettle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private FreezeCheckType checkType;
        [SerializeField] private float floorDistanceThreshold = 0.1f;
        [SerializeField] private float velocityThreshold = 0.035f;
        [SerializeField] private float startDelay = 1.0f;
        [SerializeField] private float rayCastDistance = 0.5f;
        [SerializeField] private float maxTimeToWait = 3.0f;
        [SerializeField] private float checkTime = 2.0f;
        
        [Header("Debug")]
        [SerializeField] private bool isFrozen;
        [SerializeField] private float distanceToBottom;
        [SerializeField] private float velocityMagnitude;

        [SerializeField] private bool isOnFloor;
        [SerializeField] private bool hasStoppedMoving;
        [SerializeField] private bool hasStartedMoving;
        [SerializeField] private bool isCheckStarted;
        
        internal FreezeSettledEvent OnFrozen = new FreezeSettledEvent();
        
        internal class FreezeSettledEvent : UnityEvent<GameObject, Vector3, Quaternion>
        {
        }
        
        private Rigidbody _rigidbody;
        
        private float _checkTimer;
        private float _movingTimer;
        private float _waitTimer;
        
        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            StartCoroutine(WaitToStartAsync());
        }

        /// <summary>
        /// Unity Awake method. Runs every frame so remove this if not required.
        /// </summary>
        public void Update()
        {
            if (isFrozen || !isCheckStarted)
            {
                return;
            }

            CheckObjectIsSettled();
        }

        /// <summary>
        /// Allow an external component to configure 
        /// </summary>
        internal void ConfigureParams(FreezeCheckType newCheckType, float newVelocityThreshold,
            float newRayCastDistance, float newStartDelay, float newMaxTimeToWait)
        {
            checkType = newCheckType;
            velocityThreshold = newVelocityThreshold;
            rayCastDistance = newRayCastDistance;
            startDelay = newStartDelay;
            maxTimeToWait = newMaxTimeToWait;
        }

        private void CheckObjectIsSettled()
        {
                        
            HasStartedMoving();
            if (!hasStartedMoving)
            {
                return;
            }
            
            // Only want to wait so long to freeze
            if (_waitTimer > maxTimeToWait)
            {
                Debug.Log($"Max wait threshold reached! Is {gameObject.name} really settled?");
                FreezeMovement();
            }
            _waitTimer += Time.deltaTime;
            
            // Check status
            IsOnFloor();
            HasStoppedMoving();

            if (checkType == FreezeCheckType.Velocity && hasStoppedMoving)
            {
                // Debug.Log("Velocity Threshold Reached");
                FreezeMovement();
            }

            if (checkType == FreezeCheckType.GroundCheck && isOnFloor)
            {
                // Debug.Log("Ground Threshold Reached");
                FreezeMovement();
            }

            if (checkType == FreezeCheckType.Both && hasStoppedMoving && isOnFloor)
            {
                // Debug.Log("Both Thresholds Reached");
                FreezeMovement();
            }

            if (checkType == FreezeCheckType.Either && (hasStoppedMoving || isOnFloor))
            {
                // Debug.Log($"Either Threshold Reached: HasStoppedMoving = {_hasStoppedMoving}, IsOnFloor = {_isOnFloor}");
                FreezeMovement();
            }
        }
        
        /// <summary>
        /// Determine if the object has started moving
        /// </summary>
        /// <returns></returns>
        private void HasStartedMoving()
        {
            if (hasStartedMoving)
            {
                return;
            }
            
            if (_rigidbody.velocity.magnitude > 0)
            {
                _movingTimer += Time.deltaTime;
            }
            else
            {
                _movingTimer = 0.0f;
            }

            hasStartedMoving = _movingTimer > checkTime;
        }

        /// <summary>
        /// Determines if object has stopped moving
        /// </summary>
        /// <returns></returns>
        private void HasStoppedMoving()
        {
            if (hasStoppedMoving)
            {
                return;
            }
            
            velocityMagnitude = _rigidbody.velocity.magnitude;

            if (velocityMagnitude < velocityThreshold)
            {
                _checkTimer += Time.deltaTime;
            }
            else
            {
                _checkTimer = 0.0f;
            }

            hasStoppedMoving = _checkTimer > checkTime;
        }
        
        /// <summary>
        /// Freezes the object rigidbody
        /// </summary>
        private void FreezeMovement()
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.isKinematic = true;
            isFrozen = true;
            OnFrozen?.Invoke(gameObject, transform.position, transform.rotation);
        }

        /// <summary>
        /// Wait a few seconds before starting
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitToStartAsync()
        {
            yield return new WaitForSeconds(startDelay);
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            isCheckStarted = true;
        }

        /// <summary>
        /// Determines if object is on the floor
        /// </summary>
        /// <returns></returns>
        private void IsOnFloor()
        {
            bool isHit = Physics.Raycast(transform.position, -Vector3.up, out var hit, rayCastDistance);
            distanceToBottom = Vector3.Distance(transform.position, hit.point);
            isOnFloor = isHit && distanceToBottom < floorDistanceThreshold;
        }
    }
}
