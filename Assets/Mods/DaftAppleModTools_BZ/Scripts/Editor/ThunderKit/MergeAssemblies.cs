using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Manifests.Datums;
using ThunderKit.Core.Paths;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace ThunderKit.Core.Pipelines.Jobs
{
    /// <summary>
    /// Embeds selected staged dependencies in a mod assembly using the ILRepack dotnet tool.
    /// </summary>
    [PipelineSupport(typeof(Pipeline)), ManifestProcessor, RequiresManifestDatumType(typeof(AssemblyMergeSettings))]
    public class MergeAssemblies : PipelineJob
    {
        [SerializeField, Required, Tooltip("ILRepack executable name on PATH, or its full path.")]
        private string executablePath = "ilrepack";

        [SerializeField, Required, PathReferenceResolver]
        private string stagingPath = "<ManifestPluginStaging>";

        /// <summary>
        /// Merges and validates the staged assemblies before replacing the mod DLL.
        /// </summary>
        public override async Task Execute(Pipeline pipeline)
        {
            AssemblyMergeSettings settings = GetSettings(pipeline);
            ValidateSettings(pipeline, settings);

            string primaryAssembly = GetAssemblyFileName(settings.PrimaryAssembly);
            string stageDirectory = Path.GetFullPath(PathReference.ResolvePath(stagingPath, pipeline, this));
            string primaryPath = GetAssemblyPath(stageDirectory, primaryAssembly);
            HashSet<string> inputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { primaryPath };
            List<string> dependencyPaths = new List<string>();

            foreach (AssemblyDefinitionAsset dependency in settings.Dependencies)
            {
                string dependencyAssembly = GetAssemblyFileName(dependency);
                string dependencyPath = GetAssemblyPath(stageDirectory, dependencyAssembly);
                if (!inputPaths.Add(dependencyPath))
                {
                    throw new InvalidOperationException($"Duplicate merge input: {dependencyAssembly}");
                }
                dependencyPaths.Add(dependencyPath);
            }

            // A separate output preserves staged inputs when ILRepack or validation fails.
            string mergeDirectory = Path.Combine(stageDirectory, "ILRepack-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(mergeDirectory);
            string outputPath = Path.Combine(mergeDirectory, primaryAssembly);
            try
            {
                string arguments = BuildArguments(primaryPath, dependencyPaths, outputPath, stageDirectory);
                pipeline.Log(LogLevel.Information, $"Merging dependencies into {primaryAssembly}", arguments);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    WorkingDirectory = stageDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit());
                    string output = await outputTask;
                    string error = await errorTask;
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"ILRepack exited with code {process.ExitCode}.\n{output}\n{error}");
                    }
                    pipeline.Log(LogLevel.Information, "ILRepack completed", output, error);
                }

                ValidateOutput(primaryPath, dependencyPaths, outputPath);
                File.Replace(outputPath, primaryPath, null);
                DeleteSymbols(primaryPath);
                foreach (string dependencyPath in dependencyPaths)
                {
                    File.Delete(dependencyPath);
                    DeleteSymbols(dependencyPath);
                }
                pipeline.Log(LogLevel.Information, $"Staged merged {primaryAssembly}; removed embedded dependency DLLs and stale symbols.");
            }
            finally
            {
                // Only files produced in this job's unique output directory are removed.
                foreach (string file in Directory.GetFiles(mergeDirectory))
                {
                    File.Delete(file);
                }
                Directory.Delete(mergeDirectory);
            }
        }

        private static AssemblyMergeSettings GetSettings(Pipeline pipeline)
        {
            AssemblyMergeSettings settings = null;
            foreach (ComposableElement datum in pipeline.Manifest.Data)
            {
                AssemblyMergeSettings candidate = datum as AssemblyMergeSettings;
                if (candidate is null)
                {
                    continue;
                }
                if (settings != null)
                {
                    throw new InvalidOperationException("A manifest can contain only one AssemblyMergeSettings datum.");
                }
                settings = candidate;
            }
            if (settings is null)
            {
                throw new InvalidOperationException("MergeAssemblies requires AssemblyMergeSettings on the current manifest.");
            }
            return settings;
        }

        private static void ValidateSettings(Pipeline pipeline, AssemblyMergeSettings settings)
        {
            if (!settings.PrimaryAssembly)
            {
                throw new InvalidOperationException("AssemblyMergeSettings requires a primary assembly definition.");
            }
            if (settings.Dependencies == null || settings.Dependencies.Length == 0)
            {
                throw new InvalidOperationException("AssemblyMergeSettings requires at least one dependency.");
            }

            HashSet<AssemblyDefinitionAsset> stagedDefinitions = new HashSet<AssemblyDefinitionAsset>();
            foreach (ComposableElement datum in pipeline.Manifest.Data)
            {
                AssemblyDefinitions definitions = datum as AssemblyDefinitions;
                if (definitions == null || definitions.definitions == null)
                {
                    continue;
                }
                foreach (AssemblyDefinitionAsset definition in definitions.definitions)
                {
                    if (definition)
                    {
                        stagedDefinitions.Add(definition);
                    }
                }
            }

            ValidateStagedDefinition(stagedDefinitions, settings.PrimaryAssembly, "primary");
            HashSet<AssemblyDefinitionAsset> mergeInputs = new HashSet<AssemblyDefinitionAsset>
            {
                settings.PrimaryAssembly
            };
            foreach (AssemblyDefinitionAsset dependency in settings.Dependencies)
            {
                if (!dependency)
                {
                    throw new InvalidOperationException("AssemblyMergeSettings contains an unassigned dependency.");
                }
                ValidateStagedDefinition(stagedDefinitions, dependency, "dependency");
                if (!mergeInputs.Add(dependency))
                {
                    throw new InvalidOperationException($"Duplicate merge input: {GetAssemblyFileName(dependency)}");
                }
            }
        }

        private static void ValidateStagedDefinition(HashSet<AssemblyDefinitionAsset> stagedDefinitions,
            AssemblyDefinitionAsset definition, string role)
        {
            if (!stagedDefinitions.Contains(definition))
            {
                throw new InvalidOperationException(
                    $"The {role} assembly {GetAssemblyFileName(definition)} is not included in the manifest's AssemblyDefinitions.");
            }
        }

        private static string GetAssemblyFileName(AssemblyDefinitionAsset definition)
        {
            AssemblyDefinitionData data = JsonUtility.FromJson<AssemblyDefinitionData>(definition.text);
            if (data == null || string.IsNullOrWhiteSpace(data.name))
            {
                throw new InvalidOperationException($"Assembly definition {definition.name} has no assembly name.");
            }
            return data.name + ".dll";
        }

        private static string GetAssemblyPath(string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName ||
                !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Merge inputs must be DLL filenames without directories.");
            }
            string path = Path.Combine(directory, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Run MergeAssemblies after StageAssemblies. Staged assembly not found.", path);
            }
            return path;
        }

        private static string BuildArguments(string primaryPath, List<string> dependencyPaths, string outputPath,
            string stageDirectory)
        {
            StringBuilder arguments = new StringBuilder("/internalize /ndebug /skipconfig /out:");
            arguments.Append(Quote(outputPath));
            HashSet<string> searchDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { stageDirectory };
            HashSet<string> inputAssemblyNames = new HashSet<string>(StringComparer.Ordinal)
            {
                Path.GetFileNameWithoutExtension(primaryPath)
            };
            foreach (string dependencyPath in dependencyPaths)
            {
                inputAssemblyNames.Add(Path.GetFileNameWithoutExtension(dependencyPath));
            }
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies())
            {
                if (!inputAssemblyNames.Contains(assembly.name))
                {
                    continue;
                }
                foreach (string reference in assembly.allReferences)
                {
                    searchDirectories.Add(Path.GetDirectoryName(Path.GetFullPath(reference)));
                }
            }
            foreach (string directory in searchDirectories)
            {
                arguments.Append(" /lib:").Append(Quote(directory));
            }
            arguments.Append(' ').Append(Quote(primaryPath));
            foreach (string dependencyPath in dependencyPaths)
            {
                arguments.Append(' ').Append(Quote(dependencyPath));
            }
            return arguments.ToString();
        }

        private static string Quote(string path)
        {
            // All arguments are file/directory paths, without embedded quotes or trailing separators.
            if (path.IndexOf('"') >= 0)
            {
                throw new ArgumentException("ILRepack paths cannot contain quotation marks.");
            }
            return "\"" + path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "\"";
        }

        private static void ValidateOutput(string primaryPath, List<string> dependencyPaths, string outputPath)
        {
            AssemblyName originalName = AssemblyName.GetAssemblyName(primaryPath);
            AssemblyName mergedName = AssemblyName.GetAssemblyName(outputPath);
            if (originalName.FullName != mergedName.FullName)
            {
                throw new InvalidOperationException("ILRepack changed the primary assembly identity.");
            }

            // Inspect metadata without loading executable mod code into the Unity Editor domain.
            Assembly mergedAssembly = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(outputPath));
            foreach (AssemblyName reference in mergedAssembly.GetReferencedAssemblies())
            {
                foreach (string dependencyPath in dependencyPaths)
                {
                    if (reference.Name == AssemblyName.GetAssemblyName(dependencyPath).Name)
                    {
                        throw new InvalidOperationException($"Merged assembly still references {reference.Name}.");
                    }
                }
            }
        }

        private static void DeleteSymbols(string assemblyPath)
        {
            File.Delete(Path.ChangeExtension(assemblyPath, ".pdb"));
            File.Delete(assemblyPath + ".mdb");
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
        }
    }
}
