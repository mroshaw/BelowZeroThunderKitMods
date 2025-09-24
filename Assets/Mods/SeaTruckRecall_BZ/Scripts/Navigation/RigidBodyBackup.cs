using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    internal class RigidBodyBackup
    {
        private readonly Rigidbody _rigidBody;
        private readonly float _mass;
        private readonly float _drag;
        private bool _isKinematic;
        private CollisionDetectionMode _collisionDetectionMode;
        private RigidbodyInterpolation _interpolation;

        internal RigidBodyBackup(Rigidbody rigidBody)
        {
            _rigidBody = rigidBody;
            _mass = rigidBody.mass;
            _drag = rigidBody.drag;
            _isKinematic = rigidBody.isKinematic;
            _collisionDetectionMode = rigidBody.collisionDetectionMode;
            _interpolation = rigidBody.interpolation;
        }

        internal void Zero()
        {
            if (!_rigidBody)
            {
                ModDebugLog.LogDebug("Backup RigidBody is null!");
                return;
            }

            _rigidBody.drag = 0;
            _rigidBody.mass = 0;
            _rigidBody.interpolation = RigidbodyInterpolation.None;
            _rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidBody.isKinematic = true;
            // UWE.Utils.SetIsKinematicAndUpdateInterpolation(_rigidBody, true, false);
        }

        internal void Restore()
        {
            if (!_rigidBody)
            {
                ModDebugLog.LogDebug("Backup RigidBody is null!");
                return;
            }

            _rigidBody.mass = _mass;
            _rigidBody.drag = _drag;
            _rigidBody.isKinematic = _isKinematic;
            _rigidBody.collisionDetectionMode = _collisionDetectionMode;
            _rigidBody.interpolation = _interpolation;
            // UWE.Utils.SetIsKinematicAndUpdateInterpolation(_rigidBody, false, true);
        }
    }
}