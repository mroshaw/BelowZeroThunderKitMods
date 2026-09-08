using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    public sealed class TechTypeListEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text sourceText;
        [SerializeField] private Button removeButton;
        private TechType techType;
        private Action<TechType> removed;

        internal void Bind(
            TechTypeDisplayData data,
            Action<TechType> callback)
        {
            techType = data.TechType;
            removed = callback;
            nameText.text = data.AssignedDisplayName;
            iconImage.sprite = data.Icon;
            iconImage.enabled = iconImage.sprite != null;
            sourceText.text = data.SourceName;
            sourceText.color = data.SourceColor;
        }

        /// <summary>
        /// Removes this TechType from its category.
        /// </summary>
        public void Remove()
        {
            removed?.Invoke(techType);
        }
    }
}
