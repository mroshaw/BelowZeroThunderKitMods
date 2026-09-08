using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    public sealed class CategoryListEntry : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        private int index;
        private Action<int> selected;

        internal void Bind(int categoryIndex, string categoryName, string status, Action<int> callback)
        {
            index = categoryIndex;
            selected = callback;
            nameText.text = categoryName;
            statusText.text = status;
        }

        /// <summary>
        /// Selects this category in the configuration dialog.
        /// </summary>
        public void Select()
        {
            selected?.Invoke(index);
        }
    }
}
