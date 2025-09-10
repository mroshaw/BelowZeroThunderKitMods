using TMPro;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.UserInterface
{
    /// <summary>
    /// Simple class to manage the About dialog from the Mod menu
    /// </summary>
    public class AboutCanvas : MonoBehaviour
    {
        [SerializeField] private GameObject aboutPanel;
        [SerializeField] private TMP_Text versionText;
        // Enforce a small delay to prevent immediately closing the dialog
        [SerializeField] private float delayBeforeInput = 0.5f;
        [SerializeField] private float distanceFromCamera = 1.0f;
        
        private AudioSource _audioSource;
        private bool _visible;
        private float _counter;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _counter = 0.0f;
            versionText.text = $"v{SubnauticaPetsPlugin.VersionString}";
            Hide();
        }

        internal void Show()
        {
            aboutPanel.SetActive(true);
            _audioSource.Play();
            _visible = true;
        }

        private void Hide()
        {
            aboutPanel.SetActive(false);
            _visible = false;
        }
        
        /// <summary>
        /// Close the dialog if a key is pressed
        /// </summary>
        private void Update()
        {
            if (!_visible)
            {
                return;
            }
            if (_counter < delayBeforeInput)
            {
                _counter += Time.unscaledDeltaTime;
                return;
            }
            if (Input.anyKeyDown)
            {
                Hide();
            }
        }
    }
}