# MergeAssemblies pipeline job

Add **MergeAssemblies** immediately after **StageAssemblies** (or **StageAssembliesExt**) in a ThunderKit build pipeline, before deployment or ZIP packaging. A manifest opts in by including an **AssemblyMergeSettings** datum.

Configure the merge on each mod's manifest by adding an **AssemblyMergeSettings** datum. For Vehicle Enhancements, configure:

| Manifest field | Value |
| --- | --- |
| Primary Assembly | `VehicleEnhancements_BZ.asmdef` |
| Dependencies | `DaftAppleModTools_BZ.Core.asmdef` |

The shared pipeline retains the ILRepack executable and staging path settings. The assembly filenames are derived from the selected `.asmdef` assets, so they do not depend on the manifest or mod folder name. Manifests without **AssemblyMergeSettings** skip the merge job.

Install the external tool with `dotnet tool install -g dotnet-ilrepack`. If Unity was already running when the tool was installed, use the full executable path or restart Unity so that it sees the updated PATH.

Keep both assembly definitions in the manifest's AssemblyDefinitions list and retain the existing mod-to-ModTools `.asmdef` reference. ThunderKit needs to compile and stage both inputs before merging. No generated `.csproj` edits are required.

The job runs ILRepack with `/internalize /ndebug /skipconfig`. It obtains dependency search directories from Unity's compilation references for both input assemblies. Unity, BepInEx, Harmony, Nautilus and other external dependencies are resolved but are not merged unless explicitly listed as inputs.

Output is written to a unique temporary directory within staging. Before replacement, the job checks that the output is a valid assembly, preserves the primary assembly identity, and has no remaining references to the embedded dependency assemblies. A failed merge or validation throws and stops the pipeline with the staged inputs intact. A successful merge replaces the staged mod DLL, removes the embedded dependency DLLs and stale PDB/MDB files, then removes the temporary directory. Build-cache originals are unchanged. Debug symbols are deliberately omitted.

Only opt in for helper types that should be private to the mod. Static state is copied independently into each merged mod. Unity asset bundles that refer to ModTools MonoBehaviours or ScriptableObjects by their original assembly name need separate handling; this job does not rewrite serialized asset references. Likewise, types exchanged with other mods must retain a shared assembly identity.

The job processes the current manifest context. If a pipeline builds several manifests, ThunderKit runs it once for each manifest that contains **AssemblyMergeSettings**.

Packaging contains one mod DLL after merging. Existing deployment jobs may copy over an old installation without removing its old ModTools DLL; inspect that installation separately. This job only removes dependencies from staging.
