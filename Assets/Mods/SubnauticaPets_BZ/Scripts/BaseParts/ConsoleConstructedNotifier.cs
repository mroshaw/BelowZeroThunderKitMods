using System;
using DaftAppleGames.SubnauticaPets.Utils;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{ 
    /// <summary>
    /// Allows the console to listen for Construction state changes so it can enable/disable the screen
    /// </summary>
    public class ConsoleConstructedNotifier : MonoBehaviour, IConstructable
    {
        private PetConsole _petConsole;

        private void Awake()
        {
            _petConsole = GetComponentInChildren<PetConsole>(true);
        }
        
        public bool IsDeconstructionObstacle()
        {
            return false;
        }

        public bool CanDeconstruct(out string reason)
        {
            reason = String.Empty;
            return true;
        }

        /// <summary>
        /// Set the screen state based on the new constructed state
        /// </summary>
        public void OnConstructedChanged(bool constructed)
        {
            LogUtils.LogDebug(LogArea.Utilities, $"ConstructedChanged to: {constructed} on {gameObject.name}... Enable screen");
            _petConsole.SetConstructedState(constructed);
        }
    }
}