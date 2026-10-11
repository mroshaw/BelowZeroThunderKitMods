using Sirenix.OdinInspector;
using ThunderKit.Core.Manifests;
using UnityEditorInternal;
using UnityEngine;

namespace ThunderKit.Core.Manifests.Datums
{
    /// <summary>
    /// Defines the assemblies that should be combined for a manifest build.
    /// </summary>
    public class AssemblyMergeSettings : ManifestDatum
    {
        [SerializeField, Required]
        private AssemblyDefinitionAsset primaryAssembly;

        [SerializeField, Required]
        private AssemblyDefinitionAsset[] dependencies;

        /// <summary>
        /// Gets the assembly that retains its identity after the merge.
        /// </summary>
        public AssemblyDefinitionAsset PrimaryAssembly => primaryAssembly;

        /// <summary>
        /// Gets the assemblies embedded into the primary assembly.
        /// </summary>
        public AssemblyDefinitionAsset[] Dependencies => dependencies;
    }
}
