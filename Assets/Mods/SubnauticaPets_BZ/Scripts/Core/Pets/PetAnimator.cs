using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal class PetAnimator : MonoBehaviour
    {
        private static readonly int IsMovingAnimParameter = Animator.StringToHash("IsMoving");
        private static readonly int IsSleepingAnimParameter = Animator.StringToHash("IsSleeping");

        private static readonly string[] BodyAnims = { "Sit", "Spin", "Roll", "Flinch" };
        private static readonly string[] FaceAnims =
        {
            "Eyes_Annoyed",
            "Eyes_Blink",
            "Eyes_Cry",
            "Eyes_Excited",
            "Eyes_Happy",
            "Eyes_LookDown",
            "Eyes_LookIn",
            "Eyes_LookOut",
            "Eyes_LookUp",
            "Eyes_Rabid",
            "Eyes_Sad",
            "Eyes_Shrink",
            "Eyes_Sleep",
            "Eyes_Spin",
            "Eyes_Squint",
            "Eyes_Trauma",
            "Sweat_L",
            "Sweat_R",
            "Teardrop_L",
            "Teardrop_R"
        };

        private static readonly int DeadAnim = Animator.StringToHash("Dead");

        private static readonly int EyesDead = Animator.StringToHash("Eyes_Dead");
        private static readonly int EyesSleep = Animator.StringToHash("Eyes_Sleep");
        private static readonly int EyesEcstatic = Animator.StringToHash("Eyes_Excited");
        private static readonly int EyesHappy = Animator.StringToHash("Eyes_Happy");
        private static readonly int EyesSad = Animator.StringToHash("Eyes_Sad");
        private static readonly int EyesDevastated = Animator.StringToHash("Eyes_Trauma");
        private static readonly int EyesNeutral = Animator.StringToHash("Eyes_LookOut");
        private Animator _animator;

        private int[] _bodyAnimHashKeys;
        private int[] _faceAnimHashKeys;
        private bool _inRandomAnim;

        private int _numBodyAnims;
        private int _numFaceAnims;
        private Pet _pet;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _pet = GetComponent<Pet>();

            _numBodyAnims = BodyAnims.Length;
            _numFaceAnims = FaceAnims.Length;

            _bodyAnimHashKeys = new int[_numBodyAnims];
            _faceAnimHashKeys = new int[_numFaceAnims];

            for (var currAnim = 0; currAnim < _numBodyAnims; currAnim++)
                _bodyAnimHashKeys[currAnim] = Animator.StringToHash(BodyAnims[currAnim]);

            for (var currAnim = 0; currAnim < _numFaceAnims; currAnim++)
                _faceAnimHashKeys[currAnim] = Animator.StringToHash(FaceAnims[currAnim]);
        }

        private void Update()
        {
            if (!_pet) return;

            if (_pet.IsDead && !_animator.GetBool(DeadAnim)) _animator.SetBool(DeadAnim, true);
        }

        [Button("Play Random Body Anim")]
        internal void PlayRandomBodyAnim(bool playSound)
        {
            if (!_animator || _bodyAnimHashKeys == null || _bodyAnimHashKeys.Length == 0) return;
            var animIndex = Random.Range(0, _numBodyAnims);

            if (_pet && playSound) _pet.PlaySound();

            ModDebugLog.LogDebug(
                $"{gameObject.name} is playing random body anim at index: {animIndex} ({BodyAnims[animIndex]})");
            _animator.SetTrigger(_bodyAnimHashKeys[animIndex]);
        }

        internal void PlayRandomFaceAnimForDuration(float duration)
        {
            StartCoroutine(PlayRandomFaceAnimAsync(duration));
        }

        [Button("Play Random Face Anim")]
        internal void PlayRandomFaceAnim()
        {
            if (!_animator || _faceAnimHashKeys == null || _faceAnimHashKeys.Length == 0 ||
                _animator.GetBool(IsSleepingAnimParameter)) return;
            var animIndex = Random.Range(0, _numFaceAnims);
            // ModDebugLog.LogDebug($"Playing random face anim at index: {animIndex}");
            _animator.Play(_faceAnimHashKeys[animIndex]);
        }

        private IEnumerator PlayRandomFaceAnimAsync(float duration)
        {
            _inRandomAnim = true;
            PlayRandomFaceAnim();
            yield return new WaitForSeconds(duration);
            _inRandomAnim = false;
        }

        internal void SetSleeping(bool isSleeping)
        {
            _animator.SetBool(IsSleepingAnimParameter, isSleeping);
            if (isSleeping)
                _animator.Play(EyesSleep);
            else
                _animator.Play(EyesHappy);
        }

        /// <summary>
        ///     Controls movement animation
        /// </summary>
        internal void SetMoving(bool isMoving)
        {
            _animator.SetBool(IsMovingAnimParameter, isMoving);
        }
    }
}