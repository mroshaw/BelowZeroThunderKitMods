using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Utils
{
    public enum ModMode { Adventure, Creative }

    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Subnautica Pets")]
    internal class ModConfigFile : ConfigFile
    {
        private const string AboutGameObjectName = "AboutCanvas.prefab";
        private static GameObject _aboutGameObject;

        /// <summary>
        /// Used to enable the "bonus pets" (cat, dog, seal, etc)
        /// </summary>
        [Toggle("Enable Bonus Pets (Restart Required)")]
        public bool EnableBonusPets = true;

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging")]
        [Tooltip("Use this to produce a detailed log when reporting bugs.")]
        public bool DetailedLogging = false;

        /// <summary>
        /// Allows the player to select "Adventure" mode, where they must find parts and DNA samples, cloning pets costs DNA.
        /// or "Instant Access" mode, where parts are unlocked by default, and everything costs 1 titanium.
        /// </summary>
        [Choice("Mod Mode (Restart Required")]
        public ModMode ModMode = ModMode.Adventure;

        /// <summary>
        /// Display a dialogue with mod credits.
        /// </summary>
        [Button("Credits")]
        public void ShowCredits(ButtonClickedEventArgs e)
        {
            InitAboutUi();
            _aboutGameObject.SetActive(true);
            _aboutGameObject.GetComponentInChildren<AudioSource>().Play();
        }

        /// <summary>
        /// Initialise the About UI if needs be
        /// </summary>
        private void InitAboutUi()
        {
            if (_aboutGameObject == null)
            {
                _aboutGameObject = CustomAssetBundleUtils.GetPrefabInstanceFromAssetBundle(AboutGameObjectName, true);
                _aboutGameObject.AddComponent<CloseOnAnyInput>();
                _aboutGameObject.AddComponent<ApplySnFont>();
                Vector2 newSizeDelta = new Vector2(-1000, -400);
                _aboutGameObject.GetComponentInChildren<CanvasRenderer>(true).GetComponent<RectTransform>().sizeDelta = newSizeDelta;
            }
        }
    }
}