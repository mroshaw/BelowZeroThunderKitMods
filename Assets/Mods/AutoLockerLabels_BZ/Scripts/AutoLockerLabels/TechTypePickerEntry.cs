using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    public sealed class TechTypePickerEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text sourceText;
        [SerializeField] private Toggle selectionToggle;
        private TechType techType;
        private Action<TechType, bool> selectionChanged;

        internal void Bind(
            TechTypeDisplayData data,
            bool selected,
            Action<TechType, bool> callback)
        {
            techType = data.TechType;
            selectionChanged = callback;
            nameText.text = data.DisplayName;
            sourceText.text = data.SourceName;
            sourceText.color = data.SourceColor;
            iconImage.sprite = data.Icon;
            iconImage.enabled = iconImage.sprite != null;
            selectionToggle.SetIsOnWithoutNotify(selected);
        }

        /// <summary>
        /// Reports the row's checkbox state to the category dialog.
        /// </summary>
        public void SetSelected(bool selected)
        {
            selectionChanged?.Invoke(techType, selected);
        }
    }
}
