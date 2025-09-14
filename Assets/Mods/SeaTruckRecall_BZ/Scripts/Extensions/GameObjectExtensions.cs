using System.Collections;
using UnityEngine;

namespace DaftAppleGames.SeatruckRecall_BZ.Extensions
{
    public static class GameObjectExtensions
    {

        /// <summary>
        /// Locate the named GameObject within the given Parent
        /// </summary>
        /// <returns></returns>
        public static GameObject GetNamedGameObject(this GameObject parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.name != childName)
                {
                    continue;
                }
                
                return child.gameObject;
            }

            return null;
        }
    }
}
