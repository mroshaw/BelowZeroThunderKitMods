using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    /// <summary>
    /// Simple debug class to rotate the game object to face the camera
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            transform.LookAt(_mainCamera.transform);
        }
    }
}