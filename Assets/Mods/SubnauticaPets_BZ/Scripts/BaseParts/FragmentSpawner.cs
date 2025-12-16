using DaftAppleGames.SubnauticaPets.Utils;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     FOR TESTING ONLY! DO NOT INCLUDE IN FINAL BUILD!
    ///     Spawns Fragments at the player location, to retrieve
    ///     spawn location coordinates
    /// </summary>
    internal class FragmentSpawner : MonoBehaviour
    {
        private const string ConsoleFragmentPrefabAssetName = "PetConsoleFragmentSPAWNTEST.prefab";
        private const string FabricatorFragmentPrefabAssetName = "PetFabricatorFragmentSPAWNTEST.prefab";

        private const KeyCode SpawnModifierKeyCode = KeyCode.LeftControl;
        private const KeyCode SpawnConsoleFragmentKeyCode = KeyCode.Keypad0;
        private const KeyCode SpawnFabricatorFragmentFlatKeyCode = KeyCode.Keypad1;
        private const KeyCode SpawnFabricatorFragmentUprightKeyCode = KeyCode.Keypad2;
        [Header("Settings")] [SerializeField] private Vector3 spawnOffset = new Vector3(0f, -0.5f, 0);
        [Header("Settings")] [SerializeField] private Vector3 maxSpin = new Vector3(100f, 100f, 100f);

        [SerializeField] private GameObject consoleFragmentPrefab;
        [SerializeField] private GameObject fabricatorFragmentPrefab;

        private Camera _playerCamera;

        /// <summary>
        ///     Grab the test Prefabs from the asset bundle
        /// </summary>
        private void Awake()
        {
            if (!consoleFragmentPrefab)
                consoleFragmentPrefab =
                    ModAssetUtils.GetObjectFromAssetBundle<GameObject>(ConsoleFragmentPrefabAssetName) as
                        GameObject;

            if (!fabricatorFragmentPrefab)
                fabricatorFragmentPrefab =
                    ModAssetUtils.GetObjectFromAssetBundle<GameObject>(FabricatorFragmentPrefabAssetName) as
                        GameObject;

            _playerCamera = MainCamera.camera;
        }

        /// <summary>
        ///     Look for key presses and spawn
        /// </summary>
        private void Update()
        {
            if (Input.GetKey(SpawnModifierKeyCode) && Input.GetKeyDown(SpawnConsoleFragmentKeyCode))
            {
                Debug.Log("Spawning console fragment");
                SpawnFragmentInstance(consoleFragmentPrefab, new Vector3(0, 0, 0));
            }

            if (Input.GetKey(SpawnModifierKeyCode) && Input.GetKeyDown(SpawnFabricatorFragmentFlatKeyCode))
            {
                Debug.Log("Spawning fabricator fragment");
                SpawnFragmentInstance(fabricatorFragmentPrefab, new Vector3(180, 0, 0));
            }

            if (Input.GetKey(SpawnModifierKeyCode) && Input.GetKeyDown(SpawnFabricatorFragmentUprightKeyCode))
            {
                Debug.Log("Spawning fabricator fragment");
                SpawnFragmentInstance(fabricatorFragmentPrefab, new Vector3(270, 0, 0));
            }
        }

        /// <summary>
        ///     Spawns a new test fragment instance and waits for it to settle.
        ///     Once settled, report the position and rotation in the log
        /// </summary>
        private void SpawnFragmentInstance(GameObject fragmentPrefab, Vector3 rotationEuler)
        {
            var fragmentInstance = Instantiate(fragmentPrefab);
            fragmentInstance.name = fragmentPrefab.name + "(Clone)";
            var spawnPosition = transform.position + spawnOffset;
            var spawnRotation = _playerCamera.transform.rotation;

            fragmentInstance.transform.position = spawnPosition;
            fragmentInstance.transform.rotation = spawnRotation;

            fragmentInstance.transform.Rotate(rotationEuler);

            var freeze = fragmentInstance.GetComponent<FreezeOnSettle>();
            freeze.OnFrozen.AddListener(FragmentSettled);

            // AddSpin(fragmentInstance);
        }

        /// <summary>
        ///     Report the position and rotation once the fragment has settled
        /// </summary>
        private void FragmentSettled(GameObject fragmentGameObject, Vector3 position, Quaternion rotation)
        {
            var rotationEuler = rotation.eulerAngles;
            ModDebugLog.LogInfo($"{fragmentGameObject} Settled at Position: {position}, Rotation: {rotationEuler}");
            ModDebugLog.LogInfo(
                $"new SpawnLocation(new Vector3({position.x:f2}f, {position.y:f2}f, {position.z:f2}f), new Vector3({rotationEuler.x:f2}f, {rotationEuler.y:f2}f, {rotationEuler.z:f2}f)), // warp {Camera.main.transform.position.x:f2} {Camera.main.transform.position.y:f2} {Camera.main.transform.position.z:f2}");
        }

        private void AddSpin(GameObject fragmentInstance)
        {
            var fragmentRigidbody = fragmentInstance.GetComponent<Rigidbody>();
            if (fragmentRigidbody)
            {
                var spinVector = new Vector3(Random.Range(0, maxSpin.x), Random.Range(0, maxSpin.y),
                    Random.Range(0, maxSpin.z));
                fragmentRigidbody.AddTorque(spinVector, ForceMode.VelocityChange);
            }
        }
    }
}