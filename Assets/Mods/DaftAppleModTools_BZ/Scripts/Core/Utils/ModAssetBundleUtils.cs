using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DaftAppleGames.ModTools
{
    /// <summary>
    /// Wrappers around the AssetBundle Unity methods
    /// Used to fetch assets while managing some error handling and logging
    /// </summary>
    public class ModAssetBundleUtils
    {
        private bool _isAssetBundleReady = false;
        private AssetBundle _assetBundle;
        private Assembly _modAssembly;
        private string _assetBundleName;
        private Object[] AllAssets => AssetBundle.LoadAllAssets();
        private ModLog _modLog;
        
        public ModAssetBundleUtils(string assetBundleName, Assembly modAssembly, bool loadImmediately, ModLog modLog)
        {
            _assetBundleName = assetBundleName;
            _modAssembly = modAssembly;
            _modLog = modLog;
            
            if (loadImmediately)
            {
                LoadAssetBundle();
            }
        }
        
        private AssetBundle AssetBundle
        {
            get
            {
                if (_isAssetBundleReady)
                {
                    return _assetBundle;
                }
                LoadAssetBundle();
                return _assetBundle;
            }
        }

        private void LoadAssetBundle()
        {
            if (_isAssetBundleReady)
            {
                return;
            }

            _modLog.LogDebug($"Loading Asset Bundle: {_assetBundleName}");
            string modPath = Path.GetDirectoryName(_modAssembly.Location);
            _modLog.LogDebug($"ModPath is: '{modPath}'");

            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath))
            {
                _modLog.LogError($"Cannot find asset bundle: {_assetBundleName}!!!");
                return;
            }
            string assetBundlePath = Path.Combine(modPath, $"Assets/{_assetBundleName}");
            _modLog.LogDebug($"AssetBundlePath is: '{assetBundlePath}'");

            _assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            _assetBundle.LoadAllAssets();
            _isAssetBundleReady = true;
            _modLog.LogDebug("Initialized mod asset bundle!");
        }

        /// <summary>
        /// Sometimes Textures aren't typed as Sprites in Asset Bundles
        /// and therefore need converting
        /// </summary>
        public Sprite GetSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Loads a given Game Object from Asset Bundles shipped in the Mod folder
        /// </summary>
        public Object GetObjectFromAssetBundle<T>(string objectName) where T : Object
        {
            _modLog.LogDebug($"ModUtils: Looking for object of type {typeof(T)} named {objectName} in Asset Bundle.");
            
            LoadAssetBundle();

            Object obj = _assetBundle.LoadAsset<T>(objectName);
            if (obj == null)
            {
                _modLog.LogError($"ModUtils: Couldn't find object named {objectName} of type {typeof(T)} in Asset Bundle!");
                return null;
            }
            _modLog.LogDebug($"ModUtils: Found GameObject named {objectName} in Asset Bundle.");
            return obj;
        }

        /// <summary>
        /// Instantiates an instance of a Prefab taken from the asset bundle
        /// </summary>
        public GameObject GetPrefabInstanceFromAssetBundle(string objectName, bool activeState)
        {
            GameObject obj = GetObjectFromAssetBundle<GameObject>(objectName) as GameObject;
            if (obj == null)
            {
                _modLog.LogDebug($"ModUtils: Couldn't find Prefab named {objectName} in Asset Bundle.");
                return null;
            }
            _modLog.LogDebug($"ModUtils: Found Prefab named {objectName} in Asset Bundle.");
            obj.SetActive(activeState);
            return Object.Instantiate(obj) as GameObject;
        }
    }
}