using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    public class RockPuncherPetAnimator : MonoBehaviour
    {
        private static readonly int OnSurfaceParameter = Animator.StringToHash("on_surface");
        private Animator _animator;
        
        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        private void LateUpdate()
        {
            _animator.SetBool(OnSurfaceParameter, true);
        }
    }
}