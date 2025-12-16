using DaftAppleGames.SubnauticaPets.UserInterface;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets
{
    public enum ModMode
    {
        Adventure,
        Creative
    }

    /// <summary>
    ///     Nautilus mod config class
    /// </summary>
    [Menu("Subnautica Pets")] internal class ModConfigFile : ConfigFile
    {
        private const string AboutGameObjectName = "AboutCanvas.prefab";
        private static GameObject _aboutGameObject;

        /// <summary>
        ///     Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging",
            Tooltip =
                "Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Subnautica\\Player.log")]
        public bool DetailedLogging = false;

        /// <summary>
        ///     Used to toggle the collider used to prevent pets from falling in the Moonpool
        /// </summary>
        [Toggle("Disable Moonpool Collider (Restart Required)",
            Tooltip =
                "Check this to disable the collider that prevents Pets from falling into the Moonpool. For example, if you have issues with custom vehicles being blocked.")]
        public bool DisableMoonpoolCollider = false;

        /// <summary>
        ///     Used to enable the "bonus pets" (cat, dog, seal, etc)
        /// </summary>
        [Toggle("Enable Bonus Pets (Restart Required)",
            Tooltip =
                "Check this to have the cat, seal, dog, walrus, fox, and rabbit pets available to spawn via the Pet Workbench.")]
        public bool EnableBonusPets = true;

        /// <summary>
        ///     Allows the player to select "Adventure" mode, where they must find parts and DNA samples, cloning pets costs DNA.
        ///     or "Instant Access" mode, where parts are unlocked by default, and everything costs 1 titanium.
        /// </summary>
        [Choice("Mod Mode (Restart Required)",
            Tooltip =
                "Adventure mod requires you to hunt down, scan and unlock blueprints and resources for Pets. Creative mod unlocks all Base Parts and Pets straight away.")]
        public ModMode ModMode = ModMode.Adventure;

        /// <summary>
        ///     Display a dialogue with mod credits.
        /// </summary>
        [Button("Credits")]
        public void ShowCredits(ButtonClickedEventArgs e)
        {
            InitAboutUi();
            _aboutGameObject.GetComponentInChildren<AboutCanvas>().Show();
        }

        /// <summary>
        ///     Initialise the About UI if needs be
        /// </summary>
        private void InitAboutUi()
        {
            if (_aboutGameObject == null)
                _aboutGameObject = ModAssetUtils.GetPrefabInstanceFromAssetBundle(AboutGameObjectName, true);
        }
    }
}