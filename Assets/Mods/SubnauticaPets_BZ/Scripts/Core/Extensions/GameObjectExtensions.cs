using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Extensions
{
    /// <summary>
    ///     Useful static extension methods to GameObject
    /// </summary>
    internal static class GameObjectExtensions
    {
        /// <summary>
        ///     Destroys all child components of a given type
        /// </summary>
        internal static void DestroyComponentsInChildren<T>(this GameObject gameObject)
        {
            ModDebugLog.LogDebug($"ModUtils: Destroying all components of type: {typeof(T)}");
            var components = gameObject.GetComponentsInChildren<T>(true);

            ModDebugLog.LogDebug($"ModUtils: Found {components.Length} instances to destroy");

            // Iterate through all child components and destroy them
            foreach (var component in components)
            {
                Object.Destroy(component as Object);
                ModDebugLog.LogDebug($"ModUtils: Destroyed: {component.GetType()}");
            }

            ModDebugLog.LogDebug($"ModUtils: Destroying all components of type: {typeof(T)}. Done.");
        }

        /// <summary>
        ///     Disables all components of given type
        /// </summary>
        internal static void DisableComponentsInChildren<T>(this GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<Behaviour>(true);

            // Iterate through all child components and disable them
            foreach (var component in components)
                if (component.GetType() == typeof(T))
                    component.enabled = false;

            ModDebugLog.LogDebug($"ModUtils: Disabling all components of type: {typeof(T)}. Done.");
        }

        /// <summary>
        ///     Updates all materials on the gameobject that use the oldTextureName to use the bundleTextureName
        /// </summary>
        public static void SetMaterialTexture(this GameObject targetGameObject, string oldTextureName,
            string bundleTextureName)
        {
            var renderers = targetGameObject.GetComponentsInChildren<Renderer>(true);
            var texture = ModAssetUtils.GetObjectFromAssetBundle<Texture>(bundleTextureName) as Texture;

            foreach (var renderer in renderers)
                if (renderer.material.mainTexture.name == oldTextureName)
                    renderer.material.mainTexture = texture;
        }

        /// <summary>
        ///     Applies a texture to the material on a GameObject
        /// </summary>
        public static void ApplyNewMeshTexture(this GameObject targetGameObject, string textureName,
            string gameObjectNameHint)
        {
            var renderers = targetGameObject.GetComponentsInChildren<Renderer>();

            if (gameObjectNameHint == "")
                renderers[0].material.mainTexture =
                    ModAssetUtils.GetObjectFromAssetBundle<Texture2D>(textureName) as Texture2D;
            else
                foreach (var renderer in renderers)
                    if (renderer.gameObject.name == gameObjectNameHint)
                        renderer.material.mainTexture =
                            ModAssetUtils.GetObjectFromAssetBundle<Texture2D>(textureName) as Texture2D;
        }

        /// <summary>
        ///     Sets the Layer of the GameObject, and it's children if isIncludeChildren is true
        /// </summary>
        public static void SetLayer(this GameObject targetGameObject, string layerName, bool includeChildren)
        {
            ModDebugLog.LogDebug(
                $"Layer of {targetGameObject} is currently {LayerMask.LayerToName(targetGameObject.layer)}");
            ModDebugLog.LogDebug($"Setting Layer of {targetGameObject} to {layerName}");
            targetGameObject.layer = LayerMask.NameToLayer(layerName);
            if (includeChildren)
                foreach (var child in targetGameObject.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer(layerName);

            ModDebugLog.LogDebug($"Layer of {targetGameObject} is now {LayerMask.LayerToName(targetGameObject.layer)}");
        }
    }
}