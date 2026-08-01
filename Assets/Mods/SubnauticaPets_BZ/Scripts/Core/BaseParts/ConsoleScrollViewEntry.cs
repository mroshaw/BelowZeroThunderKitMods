using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     An entry in the Pet Console scroll list view
    /// </summary>
    public class ConsoleScrollViewEntry : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;

        internal Button Button => button;

        internal void SetText(string text)
        {
            nameText.text = text;
        }

        internal void SetBackgroundColor(Color newColor)
        {
            var buttonColour = newColor;
            buttonColour.a = 128;
            backgroundImage.color = buttonColour;
        }

        internal void SetTextColor(Color newColor)
        {
            nameText.color = newColor;
        }
    }
}