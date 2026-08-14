using System;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    [Serializable]
    public class ModVersion
    {
        [SerializeField] private int major;
        [SerializeField] private int minor;
        [SerializeField] private int patch;

        public int Major => major;
        public int Minor => minor;
        public int Patch => patch;

        /// <summary>
        /// Increases the major component and resets the lower components
        /// </summary>
        public void IncrementMajor()
        {
            major++;
            minor = 0;
            patch = 0;
        }

        /// <summary>
        /// Increases the minor component and resets the patch component
        /// </summary>
        public void IncrementMinor()
        {
            minor++;
            patch = 0;
        }

        /// <summary>
        /// Increases the patch component
        /// </summary>
        public void IncrementPatch()
        {
            patch++;
        }

        /// <summary>
        /// Returns the version in semantic version format
        /// </summary>
        public override string ToString() => $"{major}.{minor}.{patch}";
    }
}
