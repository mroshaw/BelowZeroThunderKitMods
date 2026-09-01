using UnityEngine;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    public class LabelConfig : MonoBehaviour
    {
        [SerializeField] private Vector3 labelOffset;
        
        internal Vector3 LabelOffset => labelOffset;
    }
}