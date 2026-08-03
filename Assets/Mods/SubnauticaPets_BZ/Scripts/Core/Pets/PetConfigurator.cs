using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     MonoBehaviour class to save and load active Pets
    /// </summary>
    internal class PetConfigurator : MonoBehaviour
    {
        [SerializeField] private GameObject modelParent;
        [SerializeField] private string modelGameObjectName;

        public GameObject ModelParent => modelParent;
        public string ModelGameObjectName => modelGameObjectName;
    }
}