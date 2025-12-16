using System.Collections;
using DaftAppleGames.SubnauticaPets.Extensions;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal enum PetHappiness
    {
        Ecstatic,
        Happy,
        Neutral,
        Sad,
        Devastated,
        Dead
    }

    /// <summary>
    ///     MonoBehaviour component providing Pet behaviours and properties.
    ///     Can be used as a base to be inherited by Creature specific classes
    /// </summary>
    internal class Pet : MonoBehaviour
    {
        private const float DelayBeforeDestroy = 10.0f;

        private readonly RaycastHit[] _baseCheckCache = new RaycastHit[10];
        private Animator _animator;
        private BaseRoot _baseRootCache;

        private bool _canMove;
        private FMOD_CustomEmitter _fmodEmitter;

        private LiveMixin _liveMixin;
        private MoveOnGround _moveOnGround;
        private MoveOnSurface _moveOnSurface;
        private PetAnimator _petAnimator;

        // Private components
        private PetStateController _petStateController;
        private PrefabIdentifier _prefabIdentifier;
        private Ray _rayOrigin;
        private Rigidbody _rigidBody;
        private SkyApplier _skyApplier;
        private TechTag _techTag;
        // Public properties
        internal Base Base { get; set; }
        internal string BaseId => Base != null ? Base.GetComponent<PrefabIdentifier>().Id : "NO BASE!";

        /// <summary>
        ///     The TechType or type of Pet
        /// </summary>
        internal TechType TechType => _techTag.type;

        /// <summary>
        ///     The PrefabIdentifier. Used to load and re-configure saved pets
        /// </summary>
        internal string PrefabId => _prefabIdentifier.Id;

        internal bool IsDead { get; private set; }

        /// <summary>
        ///     Gets a "display friendly" version of the Pet Type for display
        /// </summary>
        internal string PetTypeString
        {
            get
            {
                if (_techTag == null)
                {
                    ModDebugLog.LogError("Pet _techTag is null!!!");
                    return "Unknown";
                }

                return _techTag.type.ToString().Substring(0, _techTag.type.ToString().Length - 3)
                    .AddSpacesInCamelCase();
            }
        }

        /// <summary>
        ///     Gets a "display friendly" version of the Pet Name
        /// </summary>
        internal string PetNameString => PetName.AddSpacesInCamelCase();

        /// <summary>
        ///     Public getter and setter for Pet Name
        /// </summary>
        internal string PetName { set; get; }

        /// <summary>
        ///     Set up new components
        /// </summary>
        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _animator.enabled = true;
            _techTag = GetComponent<TechTag>();
            _prefabIdentifier = GetComponent<PrefabIdentifier>();
            _liveMixin = GetComponent<LiveMixin>();
            _skyApplier = GetComponent<SkyApplier>();
            _fmodEmitter = GetComponent<FMOD_CustomEmitter>();
            _rigidBody = GetComponent<Rigidbody>();
            _petAnimator = GetComponent<PetAnimator>();
        }

        /// <summary>
        ///     Unity Start method
        /// </summary>
        private void Start()
        {
            // Need to avoid doing anything for the prefab
            if (!gameObject.name.Contains("Clone"))
            {
                ModDebugLog.LogDebug($"Start called on Pet Prefab: {gameObject.name}");
                return;
            }

            ModDebugLog.LogDebug($"In Pet Start for: {gameObject.name}");

            UpdateActions();
            SetMoveMethod();
            StartCoroutine(CleanUpComponentsAfterDelay());
            LoadPetData();
            DeriveBase();
            SubnauticaPetsPlugin.PetSaver.RegisterPet(this);
        }

        private void OnDisable()
        {
            SubnauticaPetsPlugin.PetSaver.UnregisterPet(this);
        }

        /// <summary>
        ///     Ensures the Pet gameobject is parented to the base
        /// </summary>
        private void ParentToBase()
        {
            if (!Base || transform.parent == Base.transform) return;

            ModDebugLog.LogDebug($"Fixing parent for {PetName}");
            transform.SetParent(Base.transform);
            _skyApplier.SetSky(Skies.BaseInterior);
        }

        /// <summary>
        ///     Determine which base the pet is currently in
        /// </summary>
        private void DeriveBase()
        {
            ModDebugLog.LogDebug($"In DeriveBase for {PetName}");
            if (Base)
            {
                ModDebugLog.LogDebug("Base already set!");
                return;
            }

            ModDebugLog.LogDebug("Base not set");

            if (transform.parent)
            {
                ModDebugLog.LogDebug("Looking for BaseRoot in parent");
                Base = transform.parent.GetComponent<Base>();
                if (Base)
                {
                    ModDebugLog.LogDebug($"Setting Base to: {Base.gameObject.name}...");
                    return;
                }
            }

            ModDebugLog.LogDebug("Looking for Base via RayCast...");

            _rayOrigin = new Ray(transform.position, transform.up);
            var numHits = Physics.RaycastNonAlloc(_rayOrigin, _baseCheckCache, 10.0f);

            ModDebugLog.LogDebug($"RayCast hit {numHits} colliders");

            for (var curHit = 0; curHit < numHits; curHit++)
            {
                ModDebugLog.LogDebug($"Looking in {_baseCheckCache[curHit].collider.gameObject.name}.");

                Base = _baseCheckCache[curHit].collider.transform.parent.GetComponentInChildren<Base>(true);

                if (Base)
                {
                    ModDebugLog.LogDebug($"Found BaseRoot in {Base.gameObject.name}");
                    return;
                }
            }

            ModDebugLog.LogError($"Can't find Base for Pet {PetName}!");
        }


        /// <summary>
        ///     Wait for data to be loaded, then update if this is a loaded pet
        /// </summary>
        internal void LoadPetData()
        {
            foreach (var petDetails in LoadedPetDetailsHashSet)
                if (petDetails.PrefabId == PrefabId)
                {
                    ModDebugLog.LogDebug($"Pet: Found {petDetails.PrefabId}, assigning Pet name");
                    PetName = petDetails.PetName;
                    break;
                }
        }

        /// <summary>
        ///     Write Pet data to the log
        /// </summary>
        private void LogPetData(string identifier)
        {
            ModDebugLog.LogDebug(
                $"{identifier}: GameObject is: {gameObject.name}, ObjectId is: {gameObject.GetInstanceID()}, " +
                $"Transform is: ({gameObject.transform.position.x},{gameObject.transform.position.y}, {gameObject.transform.position.z}), Type is: {PetTypeString}, PrefabId is: {PrefabId}");
        }

        /// <summary>
        ///     Clean up Constructable components. This prevents the Pet from being targetted by the Builder for "Deconstruction"
        /// </summary>
        private void CleanUpComponents()
        {
            gameObject.DestroyComponentsInChildren<Constructable>();
            gameObject.DestroyComponentsInChildren<Pickupable>();
        }

        private IEnumerator CleanUpComponentsAfterDelay()
        {
            yield return new WaitForSeconds(2.0f);
            CleanUpComponents();
        }

        /// <summary>
        ///     Sets the method of movement based on what components are preset
        /// </summary>
        private void SetMoveMethod()
        {
            _moveOnSurface = GetComponent<MoveOnSurface>();
            if (!_moveOnSurface)
            {
                // ModDebugLog.LogDebug("Pet: No MoveOnSurface component found.");
            }

            _moveOnGround = GetComponent<MoveOnGround>();
            if (!_moveOnGround)
            {
                // ModDebugLog.LogDebug("Pet: No MoveOnGround component found.");
            }

            _petStateController = GetComponent<PetStateController>();
            if (!_petStateController)
            {
                // ModDebugLog.LogDebug("Pet: No PetStateController component found.");
            }

            _canMove = _moveOnGround || _moveOnSurface || _petStateController;

            if (!_canMove) ModDebugLog.LogDebug("Pet: No ground movement behaviour found. Cannot move to player!");
        }

        internal void PlaySound()
        {
            if (_fmodEmitter) _fmodEmitter.Play();
        }

        /// <summary>
        ///     Play a pet animation
        /// </summary>
        internal void PlayAnimation()
        {
            if (!_petAnimator && _animator)
            {
                _animator.SetTrigger("flinch");
                PlaySound();
            }
            else if (_petAnimator)
            {
                _petAnimator.PlayRandomBodyAnim(true);
                _petAnimator.PlayRandomFaceAnim();
            }
            else
            {
                ModDebugLog.LogError("Pet: No animator found, so can't play animation.");
            }
        }

        /// <summary>
        ///     Move the pet towards the player location
        /// </summary>
        internal void MoveToPlayer()
        {
            if (!_canMove)
            {
                ModDebugLog.LogError("Pet: No movement component found, so can't walk to player");
                return;
            }

            if (_moveOnSurface)
            {
                _moveOnSurface.walkBehaviour.GoToInternal(Player.main.transform.position,
                    (Player.main.transform.position - transform.position).normalized, _moveOnSurface.moveVelocity);
                return;
            }

            if (_moveOnGround)
            {
                _moveOnGround.swimBehaviour.GoToInternal(Player.main.transform.position,
                    (Player.main.transform.position - transform.position).normalized, _moveOnGround.swimVelocity);
                return;
            }

            if (_petStateController) _petStateController.MoveToPosition(Player.main.transform.position);
        }

        /// <summary>
        ///     Rescans the creature for valid actions. Used after adding or removing
        ///     new actions.
        /// </summary>
        internal void UpdateActions()
        {
            var creature = GetComponent<Creature>();
            if (creature) creature.ScanCreatureActions();
        }

        /// <summary>
        ///     Listen for the OnKill message
        /// </summary>
        internal void OnKill()
        {
            if (IsDead) return;

            IsDead = true;
            _rigidBody.isKinematic = true;
            if (_petStateController) _petStateController.Kill();

            SubnauticaPetsPlugin.PetSaver.UnregisterPet(this);
            ModDebugLog.LogDebug($"Picked up the OnKill message in {gameObject.name}");

            if (!string.IsNullOrEmpty(PetNameString))
                ErrorMessage.AddMessage(
                    $"{Language.main.Get("Alert_PetDeadFarewell")} {PetNameString}! {Language.main.Get("Alert_PetDeadGoodBoy")} {PetTypeString}!");

            // Destroy GameObject after delay
            StartCoroutine(DestroyAfterDelay());
        }

        /// <summary>
        ///     Kills the pet by applying damage
        /// </summary>
        internal void Kill()
        {
            if (_liveMixin) _liveMixin.Kill();
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(DelayBeforeDestroy);
            Destroy(gameObject);
        }

        /// <summary>
        ///     Find and Kill all pets. For use to clear out all pets in case
        ///     of some sort of catastrophic failure.
        /// </summary>
        internal static void KillAllPets()
        {
            foreach (var pet in FindObjectsOfType<Pet>()) pet.Kill();
        }

        /// <summary>
        ///     Static method to determine if the given TechType is a Pet or not
        /// </summary>
        internal static bool IsPetTechType(TechType techType)
        {
            return techType == PetPrefabs.PenglingBabyPrefab.Info.TechType ||
                   techType == PetPrefabs.PengwingAdultPrefab.Info.TechType ||
                   techType == PetPrefabs.SnowstalkerBabyPrefab.Info.TechType ||
                   techType == PetPrefabs.PinnacaridPrefab.Info.TechType ||
                   techType == PetPrefabs.TrivalveYellowPrefab.Info.TechType ||
                   techType == PetPrefabs.TrivalveBluePrefab.Info.TechType ||
                   techType == CustomPetPrefabs.CatPetPrefab.Info.TechType ||
                   techType == CustomPetPrefabs.DogPetPrefab.Info.TechType ||
                   techType == CustomPetPrefabs.RabbitPetPrefab.Info.TechType ||
                   techType == CustomPetPrefabs.SealPetPrefab.Info.TechType ||
                   techType == CustomPetPrefabs.WalrusPetPrefab.Info.TechType ||
                   techType == CustomPetPrefabs.FoxPetPrefab.Info.TechType;
        }
    }
}