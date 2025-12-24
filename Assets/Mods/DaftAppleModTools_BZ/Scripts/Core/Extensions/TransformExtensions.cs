using UnityEngine;

namespace DaftAppleGames.ModTools.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Sets the transform local position, rotation and scale to "zero"
        /// </summary>
        public static void LocalZero(this Transform transform)
        {
            transform.localPosition =  Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}