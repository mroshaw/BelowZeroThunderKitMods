using System;
using System.Collections;
using DaftAppleGames.SubnauticaPets.Pets;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     MonoBehaviour for the Pet Fabricator
    /// </summary>
    internal class PetFabricator : MonoBehaviour
    {
        private GameObject _baseParentGameObject;

        private Vector3 _spawnPoint;
        // This is the base root of the base n which the fabricator was created
        internal Base Base { get; set; }

        internal string BaseId
        {
            get
            {
                if (Base != null) return Base.GetComponent<PrefabIdentifier>().Id;

                return "NO BASE!";
            }
        }

        /// <summary>
        ///     Initialise the component
        /// </summary>
        private void Start()
        {
            if (transform.parent == null)
                // We're probably in the prefab, so return.
                return;

            var ghostModel = GetComponent<CrafterGhostModel>();
            if (ghostModel != null) _spawnPoint = ghostModel.itemSpawnPoint.position;

            SetParentBaseObject();
            _baseParentGameObject = gameObject.transform.parent.gameObject;
        }

        private void SetParentBaseObject()
        {
            // Get the BasePart transform
            Base = transform.parent.GetComponent<Base>();
            if (Base)
                ModDebugLog.LogDebug($"PetFabriactor Start in Base: {Base.gameObject.name}");
            else
                ModDebugLog.LogDebug("PetFabriactor Start: Base not found in parent!");
        }

        /// <summary>
        ///     Spawn a Pet. Optinal callback action is invoked, if provided, with the spawned GameObject
        /// </summary>
        internal void SpawnPet(TechType techType, Action<GameObject> callBack = null)
        {
            StartCoroutine(SpawnPetAsync(techType, callBack));
        }

        /// <summary>
        ///     Spawn Pet Async version. Optional callback is invoked at the end of the process, if provided.
        /// </summary>
        private IEnumerator SpawnPetAsync(TechType techType, Action<GameObject> callback = null)
        {
            var task = CraftData.GetPrefabForTechTypeAsync(techType);
            yield return task;
            var prefab = task.GetResult();
            prefab.SetActive(false);
            // Instantiate in the spawn position
            ModDebugLog.LogDebug($"PetFabricator: Instantiating Pet {techType}");
            var newPetGameObject = Instantiate(prefab, _spawnPoint, Quaternion.identity);

            ModDebugLog.LogDebug("PetFabricator: Instantiating Pet done!");
            var newPet = newPetGameObject.GetComponent<Pet>();
            if (newPet)
            {
                ModDebugLog.LogDebug("PetFabricator: Setting Pet Name...");
                newPet.PetName = $"Test Subject {SubnauticaPetsPlugin.PetSaver.PetList.Count + 1}";
                ModDebugLog.LogDebug("PetFabricator: Setting Pet Name... Done.");

                // Tell the pet which base it belongs to and parent the transform
                newPet.Base = Base;
                newPet.transform.SetParent(Base.transform);
                PetPrefabConfigUtils.ConfigureSkyApplier(newPetGameObject);
            }
            else
            {
                ModDebugLog.LogError("PetFabricator: Spawned Pet has no Pet component!");
            }

            // Rotate to face the player
            newPetGameObject.transform.LookAt(Player.main.transform.position);
            newPetGameObject.SetActive(true);
            SimpleMovement simpleMovement = newPetGameObject.GetComponent<SimpleMovement>();
            if (simpleMovement)
                simpleMovement.BeginSpawnSettlement(
                    Player.main.transform.position - newPetGameObject.transform.position);
            newPet.LoadPetData();

            callback?.Invoke(newPetGameObject);
        }
    }
}
