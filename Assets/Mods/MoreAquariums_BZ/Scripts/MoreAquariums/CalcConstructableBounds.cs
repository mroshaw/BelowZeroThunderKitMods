using Sirenix.OdinInspector;
using UnityEngine;

namespace DaftAppleGames.MoreAquariums
{
    public class CalcConstructableBounds : MonoBehaviour
    {
        [SerializeField] private GameObject renderersObject;
        
        [Button("Set Constructable Bounds")]
        private void SetConstructableBounds()
        {
            ConstructableBounds constructableBounds = GetComponent<ConstructableBounds>();
            OrientedBounds.EncapsulateRenderers(renderersObject.transform, renderersObject, new Quaternion(0f, 0f, 0f, 0f), out Vector3 boundsCenter, out Vector3 boundsExtents);
            constructableBounds.bounds = new OrientedBounds(boundsCenter, new Quaternion(0, 0, 0,0), boundsExtents);
        }
    }
}