using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaftAppleGames.ModUtils
{
    public class TextMeshProColorSwap : MonoBehaviour
    {
        [SerializeField] private bool resetToWhiteOnDisable;
        
        // Token: 0x06003A81 RID: 14977 RVA: 0x00138244 File Offset: 0x00136444
        private void OnDisable()
        {
            if (resetToWhiteOnDisable)
            {
                MakeTextWhite();
            }
        }

        /// <summary>
        /// Make all child TMP text black
        /// </summary>
        public void MakeTextBlack()
        {
            TextMeshProUGUI[] componentsInChildren = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (TextMeshProUGUI component in componentsInChildren)
            {
                // Check if parent is interactable
                Transform parentTransform = component.transform.parent;
                if (parentTransform)
                {
                    if (parentTransform.gameObject.TryGetComponent<Selectable>(out var selectable))
                    {
                        if (!selectable.interactable)
                        {
                            return;
                        }
                    }
                }
                component.color = Color.black;
            }
        }

        /// <summary>
        /// Make all child TMP text white
        /// </summary>
        public void MakeTextWhite()
        {
            TextMeshProUGUI[] componentsInChildren = GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].color = Color.white;
            }
        }
    }
}