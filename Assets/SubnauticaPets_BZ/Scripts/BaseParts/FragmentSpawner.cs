using DaftAppleGames.SubnauticaPets.Utils;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    /// FOR TESTING ONLY! DO NOT INCLUDE IN FINAL BUILD!
    /// Spawns Fragments at the player location, to retrieve
    /// spawn location coordinates
    /// </summary>
    internal class FragmentSpawner : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private Vector3 spawnOffset = new Vector3(0f, -0.5f, 0);
        [Header("Settings")] [SerializeField] private Vector3 maxSpin = new Vector3(100f, 100f, 100f);
        
        [SerializeField] private GameObject consoleFragmentPrefab;
        [SerializeField] private GameObject fabricatorFragmentPrefab;
        
        private const string ConsoleFragmentPrefabAssetName = "PetConsoleFragmentSPAWNTEST.prefab";
        private const string FabricatorFragmentPrefabAssetName = "PetFabricatorFragmentSPAWNTEST.prefab";

        private const KeyCode SpawnModifierKeyCode = KeyCode.LeftControl;
        private const KeyCode SpawnConsoleFragmentKeyCode = KeyCode.Keypad0;
        private const KeyCode SpawnFabricatorFragmentKeyCode = KeyCode.Keypad1;

        private Camera _playerCamera;
        
        /// <summary>
        /// Grab the test Prefabs from the asset bundle
        /// </summary>
        private void Awake()
        {
            if (!consoleFragmentPrefab)
            {
                consoleFragmentPrefab = CustomAssetBundleUtils.GetObjectFromAssetBundle<GameObject>(ConsoleFragmentPrefabAssetName) as GameObject;
            }

            if (!fabricatorFragmentPrefab)
            {
                fabricatorFragmentPrefab = CustomAssetBundleUtils.GetObjectFromAssetBundle<GameObject>(FabricatorFragmentPrefabAssetName) as GameObject;
            }
            
            _playerCamera = MainCamera.camera;
        }

        /// <summary>
        /// Look for key presses and spawn
        /// </summary>
        private void Update()
        {
            if (Input.GetKey(SpawnModifierKeyCode) && Input.GetKeyDown(SpawnConsoleFragmentKeyCode))
            {
                Debug.Log("Spawning console fragment");
                SpawnFragmentInstance(consoleFragmentPrefab);
            }

            if (Input.GetKey(SpawnModifierKeyCode) && Input.GetKeyDown(SpawnFabricatorFragmentKeyCode))
            {
                Debug.Log("Spawning fabricator fragment");
                SpawnFragmentInstance(fabricatorFragmentPrefab);
            }
        }

        /// <summary>
        /// Spawns a new test fragment instance and waits for it to settle.
        /// Once settled, report the position and rotation in the log
        /// </summary>
        private void SpawnFragmentInstance(GameObject fragmentPrefab)
        {
            GameObject fragmentInstance = Instantiate(fragmentPrefab);
            fragmentInstance.name = fragmentPrefab.name + "(Clone)";
            Vector3 spawnPosition = transform.position + spawnOffset;
            Quaternion  spawnRotation = _playerCamera.transform.rotation;
            
            fragmentInstance.transform.position = spawnPosition;
            fragmentInstance.transform.rotation = spawnRotation;
            
            FreezeOnSettle freeze = fragmentInstance.GetComponent<FreezeOnSettle>();
            freeze.OnFrozen.AddListener(FragmentSettled);

            // AddSpin(fragmentInstance);
        }
        
        /// <summary>
        /// Report the position and rotation once the fragment has settled
        /// </summary>
        private void FragmentSettled(GameObject fragmentGameObject, Vector3 position, Quaternion rotation)
        {
            LogUtils.LogInfo($"{fragmentGameObject} Settled at Position: {position}, Rotation: {rotation}");
            LogUtils.LogInfo($"new SpawnLocation(new Vector3({position.x:f2}f, {position.y:f2}f, {position.z:f2}f), new Vector3({rotation.x:f2}f, {rotation.y:f2}f, {rotation.z:f2}f)), // warp {position.x:f2} {position.y:f2} {position.z:f2}");
        }

        private void AddSpin(GameObject fragmentInstance)
        {
            Rigidbody fragmentRigidbody = fragmentInstance.GetComponent<Rigidbody>();
            if (fragmentRigidbody)
            {
                Vector3 spinVector = new Vector3(Random.Range(0, maxSpin.x), Random.Range(0, maxSpin.y), Random.Range(0, maxSpin.z));
                fragmentRigidbody.AddTorque(spinVector, ForceMode.VelocityChange);
            }
        }
    }
}
