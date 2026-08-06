using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     MonoBehaviour class to save and load active Pets
    /// </summary>
    internal class PetSaver : MonoBehaviour
    {
        public List<Pet> PetList = new List<Pet>();
        internal UnityEvent PetListUpdatedEvent = new UnityEvent();

        internal UnityEvent<Pet> PetRegisteredEvent = new OnPetRegisteredEvent();
        internal UnityEvent<Pet> PetUnregisteredEvent = new OnPetUnregisteredEvent();

        private void OnEnable()
        {
            Init();
            SceneManager.sceneLoaded += SceneLoadedHandler;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoadedHandler;
        }

        private void SceneLoadedHandler(Scene scene, LoadSceneMode loadSceneMode)
        {
            ModDebugLog.LogDebug($"Scene Loaded: {scene.name}");
            if (scene.name == "MenuEnvironment") ClearPetList();
        }

        /// <summary>
        ///     Initialise the Saver
        /// </summary>
        internal void Init()
        {
            PetList = new List<Pet>();
        }

        /// <summary>
        ///     Register a new Pet to the HashList
        /// </summary>
        /// <param name="pet"></param>
        internal void RegisterPet(Pet pet)
        {
            if (PetList == null) PetList = new List<Pet>();

            if (!PetList.Contains(pet) && !string.IsNullOrEmpty(pet.PetName))
            {
                PetList.Add(pet);
                ModDebugLog.LogDebug($"PetSaver: Added Pet: {pet.PetName}");
                PetRegisteredEvent?.Invoke(pet);
                PetListUpdatedEvent.Invoke();
            }
        }

        /// <summary>
        ///     Remove a Pet from the HashList
        /// </summary>
        /// <param name="pet"></param>
        internal void UnregisterPet(Pet pet, bool removeSavedDetails = false)
        {
            if (PetList.Contains(pet))
            {
                PetList.Remove(pet);
                ModDebugLog.LogDebug($"PetSaver: Removed Pet: {pet.PetName}");
                PetUnregisteredEvent.Invoke(pet);
                PetListUpdatedEvent.Invoke();
            }

            if (LoadedPetDetailsHashSet == null)
                LoadedPetDetailsHashSet = new HashSet<PetDetails>();

            LoadedPetDetailsHashSet.RemoveWhere(details => details.PrefabId == pet.PrefabId);
            if (!removeSavedDetails && pet && !string.IsNullOrEmpty(pet.PetName))
                LoadedPetDetailsHashSet.Add(new PetDetails(pet.PrefabId, pet.PetName, pet.PetTypeString));
        }

        /// <summary>
        ///     Iterate the Pet List and kill
        /// </summary>
        internal void KillAllPets()
        {
            foreach (var pet in PetList.ToArray()) pet.Kill();
            PetListUpdatedEvent.Invoke();
        }

        internal void ForceRefresh()
        {
            PetListUpdatedEvent.Invoke();
        }

        /// <summary>
        ///     Creates a HashSet of current pets, suitable using in a save game
        /// </summary>
        /// <returns></returns>
        internal HashSet<PetDetails> GetPetListAsHashSet()
        {
            var hashSet = new HashSet<PetDetails>();

            if (LoadedPetDetailsHashSet != null)
            {
                foreach (PetDetails petDetails in LoadedPetDetailsHashSet)
                    hashSet.Add(petDetails);
            }

            if (PetList == null) PetList = new List<Pet>();

            foreach (Pet pet in PetList)
                if (pet)
                {
                    hashSet.RemoveWhere(details => details.PrefabId == pet.PrefabId);
                    PetDetails newPetDetails = new PetDetails(pet.PrefabId, pet.PetName, pet.PetTypeString);
                    hashSet.Add(newPetDetails);
                }

            return hashSet;
        }

        /// <summary>
        ///     Load and update Pets
        /// </summary>
        internal void LoadData()
        {
            StartCoroutine(WaitForDataLoad());
        }

        /// <summary>
        ///     Wait for the world to settle, then init pets
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForDataLoad()
        {
            LargeWorldStreamer streamer = null;

            while (streamer == null)
            {
                streamer = FindObjectOfType<LargeWorldStreamer>();
                yield return new WaitForEndOfFrame();
            }

            while (!streamer.IsWorldSettled()) yield return new WaitForEndOfFrame();
            FixPetLoadData();
        }

        /// <summary>
        ///     Iterate through and Init pets loaded, once scene is loaded
        /// </summary>
        private void FixPetLoadData()
        {
            ModDebugLog.LogDebug("Loading Pet Data...");
            foreach (var pet in FindObjectsOfType<Pet>()) pet.LoadPetData();
        }

        /// <summary>
        ///     Removes everything from the Pet List
        /// </summary>
        private void ClearPetList()
        {
            if (PetList != null) PetList.Clear();
        }

        /// <summary>
        ///     Abstract instance stub for UnityEvent
        /// </summary>
        internal class OnPetRegisteredEvent : UnityEvent<Pet>
        {
        }

        /// <summary>
        ///     Abstract instance stub for UnityEvent
        /// </summary>
        internal class OnPetUnregisteredEvent : UnityEvent<Pet>
        {
        }

        /// <summary>
        ///     Internal PetDetails class, used to store "minimum" attributes for a pet
        ///     so we can serialize and deserialize for saving and loading pet data
        /// </summary>
        internal class PetDetails
        {
            /// <summary>
            ///     Constructor
            /// </summary>
            /// <param name="prefabId"></param>
            /// <param name="petName"></param>
            /// <param name="petType"></param>
            public PetDetails(string prefabId, string petName, string petType)
            {
                PrefabId = prefabId;
                PetName = petName;
                PetType = petType;
            }

            public string PrefabId { get; }
            public string PetName { get; set; }
            public string PetType { get; set; }
        }
    }
}
