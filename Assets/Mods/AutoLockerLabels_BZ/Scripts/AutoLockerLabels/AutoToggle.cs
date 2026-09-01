using UnityEngine;
using UnityEngine.UI;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{

    public class AutoToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Vector3 anchoredOffset;
        [SerializeField] private Vector2 size;
        internal Toggle Toggle => toggle;
        internal Vector3 AnchoredOffset => anchoredOffset;
        internal Vector2 Size => size;
    }
}