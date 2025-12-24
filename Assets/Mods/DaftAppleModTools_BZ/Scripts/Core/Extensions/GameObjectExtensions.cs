using UnityEngine;

 namespace DaftAppleGames.ModTools.Extensions
{
    /// <summary>
    /// Useful static extension methods to GameObject
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Destroys all child components of a given type
        /// </summary>
        public static void DestroyComponentsInChildren<T>(this GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<T>(true);

            // Iterate through all child components and destroy them
            foreach (var component in components)
            {
                Object.Destroy(component as Object);
            }
        }

        /// <summary>
        /// Disables all components of given type
        /// </summary>
        public static void DisableComponentsInChildren<T>(this GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<Behaviour>(true);

            // Iterate through all child components and disable them
            foreach (Behaviour component in components)
            {
                if (component.GetType() == typeof(T))
                {
                    component.enabled = false;
                }
            }
        }

        /// <summary>
        /// Updates all materials on the gameobject that use the oldTextureName to use the bundleTextureName
        /// </summary>
        public static void SetMaterialTexture(this GameObject targetGameObject, string oldTextureName, string bundleTextureName, ModAssetBundleUtils modAssetUtils)
        {
            Renderer[] renderers = targetGameObject.GetComponentsInChildren<Renderer>(true);
            Texture texture = modAssetUtils.GetObjectFromAssetBundle<Texture>(bundleTextureName) as Texture;

            foreach (Renderer renderer in renderers)
            {
                if (renderer.material.mainTexture.name == oldTextureName)
                {
                    renderer.material.mainTexture = texture;
                }
            }
        }

        /// <summary>
        /// Applies a texture to the material on a GameObject
        /// </summary>
        public static void ApplyNewMeshTexture(this GameObject targetGameObject, string textureName, string gameObjectNameHint, ModAssetBundleUtils modAssetUtils)
        {
            Renderer[] renderers = targetGameObject.GetComponentsInChildren<Renderer>();

            if (gameObjectNameHint == "")
            {
                renderers[0].material.mainTexture = modAssetUtils.GetObjectFromAssetBundle<Texture2D>(textureName) as Texture2D;
            }
            else
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.name == gameObjectNameHint)
                    {
                        renderer.material.mainTexture = modAssetUtils.GetObjectFromAssetBundle<Texture2D>(textureName) as Texture2D;
                    }
                }
            }
        }

        public static void SetLayerByIndex(this GameObject targetGameObject, int layerIndex, bool includeChildren)
        {
            targetGameObject.layer = layerIndex;
            if (includeChildren)
            {
                foreach (Transform child in targetGameObject.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = layerIndex;
                }
            }
        }
        
        /// <summary>
        /// Sets the Layer of the GameObject, and it's children if isIncludeChildren is true
        /// </summary>
        public static void SetLayer(this GameObject targetGameObject, string layerName, bool includeChildren)
        {
            targetGameObject.layer = LayerMask.NameToLayer(layerName);
            if (includeChildren)
            {
                foreach (Transform child in targetGameObject.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = LayerMask.NameToLayer(layerName);
                }
            }
        }
    }
}