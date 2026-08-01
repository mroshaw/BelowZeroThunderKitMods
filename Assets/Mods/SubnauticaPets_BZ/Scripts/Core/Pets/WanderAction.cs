using UnityEngine;
using Random = UnityEngine.Random;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Simple movement using Unity CharacterController
    /// </summary>
    internal class WanderAction : PetAction
    {
        private const float CornerDetectionWindow = 0.75f;
        private const float DistinctWallNormalDot = 0.75f;

        [Header("Action Settings")] [SerializeField] private float minTravelDistance = 2.0f;
        [SerializeField] private float maxTravelDistance = 10.0f;
        [SerializeField] private float minTravelAngle = 30.0f;
        [SerializeField] private float maxTravelAngle = 140.0f;

        private float avoidanceTurnSign;
        private Vector3 lastWallNormal;
        private float lastWallHitTime;
        private SimpleMovement _simpleMovement;

        internal override void Init()
        {
            _simpleMovement = GetComponent<SimpleMovement>();
        }

        internal override void StartAction()
        {
            avoidanceTurnSign = Random.value < 0.5f ? -1.0f : 1.0f;
            lastWallNormal = Vector3.zero;

            // Pick a random target and move
            var newTarget = GetNewTargetPosition(transform.forward);
            _simpleMovement.onArrived.AddListener(ArrivedAtTarget);
            _simpleMovement.OnHitObstacle.AddListener(HitObstacle);
            _simpleMovement.OnUnsafeBoundary.AddListener(HitBoundary);
            _simpleMovement.MoveToNewTarget(newTarget);
        }

        internal override void EndAction()
        {
            _simpleMovement.onArrived.RemoveListener(ArrivedAtTarget);
            _simpleMovement.OnHitObstacle.RemoveListener(HitObstacle);
            _simpleMovement.OnUnsafeBoundary.RemoveListener(HitBoundary);
            _simpleMovement.Stop();
        }

        private void ArrivedAtTarget()
        {
            ActionCompleted();
        }

        private void HitObstacle(Vector3 direction)
        {
            Vector3 horizontalNormal = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            bool hitCorner = lastWallNormal != Vector3.zero &&
                             Time.time - lastWallHitTime <= CornerDetectionWindow &&
                             Vector3.Dot(lastWallNormal, horizontalNormal) < DistinctWallNormalDot;

            Vector3 newTarget = hitCorner
                ? GetCornerAvoidanceTargetPosition(lastWallNormal, horizontalNormal)
                : GetWallAvoidanceTargetPosition(horizontalNormal);

            lastWallNormal = hitCorner ? Vector3.zero : horizontalNormal;
            lastWallHitTime = Time.time;
            _simpleMovement.MoveToNewTarget(newTarget);
        }

        private void HitBoundary(Vector3 safeDirection)
        {
            lastWallNormal = Vector3.zero;
            Vector3 newTarget = GetBoundaryAvoidanceTargetPosition(safeDirection);
            _simpleMovement.MoveToNewTarget(newTarget);
        }

        /// <summary>
        ///     Replaces the current wander target with a known safe position.
        /// </summary>
        internal void MoveToSafePosition(Vector3 safePosition)
        {
            _simpleMovement.MoveToNewTarget(safePosition);
        }

        internal override void UpdateAction()
        {
        }

        private Vector3 GetNewTargetPosition(Vector3 direction)
        {
            float distance = Random.Range(minTravelDistance, maxTravelDistance);
            float angle = Random.Range(minTravelAngle, maxTravelAngle);
            float sign = Random.value < 0.5f ? -1.0f : 1.0f;
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (horizontalDirection == Vector3.zero) horizontalDirection = transform.forward;

            Vector3 targetDirection = Quaternion.AngleAxis(angle * sign, Vector3.up) * horizontalDirection;
            return transform.position + targetDirection * distance;
        }

        private Vector3 GetWallAvoidanceTargetPosition(Vector3 wallNormal)
        {
            const float OutwardBias = 0.35f;

            Vector3 horizontalNormal = Vector3.ProjectOnPlane(wallNormal, Vector3.up).normalized;
            if (horizontalNormal == Vector3.zero) horizontalNormal = -transform.forward;

            Vector3 firstTangent = Vector3.Cross(Vector3.up, horizontalNormal).normalized;
            Vector3 secondTangent = -firstTangent;
            Vector3 tangent = Vector3.Dot(firstTangent, transform.forward) >=
                              Vector3.Dot(secondTangent, transform.forward)
                ? firstTangent
                : secondTangent;

            Vector3 targetDirection = (tangent + horizontalNormal * OutwardBias).normalized;
            float distance = GetAvoidanceDistance();
            return transform.position + targetDirection * distance;
        }

        private Vector3 GetBoundaryAvoidanceTargetPosition(Vector3 safeDirection)
        {
            const float SafeDirectionBias = 0.5f;

            Vector3 horizontalSafeDirection = Vector3.ProjectOnPlane(safeDirection, Vector3.up).normalized;
            if (horizontalSafeDirection == Vector3.zero) horizontalSafeDirection = -transform.forward;

            Vector3 approachDirection = -horizontalSafeDirection;
            Vector3 sideDirection = Quaternion.AngleAxis(90.0f * avoidanceTurnSign, Vector3.up) * approachDirection;
            Vector3 targetDirection = (sideDirection + horizontalSafeDirection * SafeDirectionBias).normalized;
            float distance = GetAvoidanceDistance();
            return transform.position + targetDirection * distance;
        }

        private Vector3 GetCornerAvoidanceTargetPosition(Vector3 firstNormal, Vector3 secondNormal)
        {
            Vector3 targetDirection = (firstNormal + secondNormal).normalized;
            if (targetDirection == Vector3.zero) targetDirection = -transform.forward;

            return transform.position + targetDirection * GetAvoidanceDistance();
        }

        private float GetAvoidanceDistance()
        {
            const float MaxAvoidanceDistance = 4.0f;

            return Random.Range(minTravelDistance, Mathf.Min(maxTravelDistance, MaxAvoidanceDistance));
        }
    }
}
