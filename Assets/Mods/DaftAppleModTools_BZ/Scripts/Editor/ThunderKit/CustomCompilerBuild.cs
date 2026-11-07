using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ThunderKit.Core.Pipelines.Jobs
{
    internal sealed class CustomCompilerBuild
    {
        private readonly Type coreType;
        private readonly object settings;
        private readonly object compiler;
        private readonly string asmdefPath;

        private CustomCompilerBuild(Type coreType, object settings, object compiler, string asmdefPath)
        {
            this.coreType = coreType;
            this.settings = settings;
            this.compiler = compiler;
            this.asmdefPath = asmdefPath;
        }

        internal static CustomCompilerBuild TryCreate(string asmdefPath)
        {
            // The package's configuration API is internal. Keep this bridge in one place
            // rather than duplicating its compiler, language, nullable and analyzer settings.
            Type core = Type.GetType("Coffee.CSharpCompilerSettings.Core, CSharpCompilerSettings", false);
            if (core is null)
            {
                return null;
            }
            Type settingsType = core.Assembly.GetType("Coffee.CSharpCompilerSettings.CscSettingsAsset", true);
            object settings = settingsType.GetMethod("GetAtPath").Invoke(null, new object[] { asmdefPath });
            if (settings is null)
            {
                settings = settingsType.GetProperty("instance").GetValue(null);
            }
            bool custom = (bool)settingsType.GetMethod("ShouldToUseCustomCompiler")
                .Invoke(settings, new object[] { asmdefPath });
            if (!custom)
            {
                return null;
            }
            object package = settingsType.GetProperty("CompilerPackage").GetValue(settings);
            object packageId = package.GetType().GetProperty("PackageId").GetValue(package);
            Type compilerType = core.Assembly.GetType("Coffee.CSharpCompilerSettings.CompilerInfo", true);
            object compiler = compilerType.GetMethod("GetInstalledInfo").Invoke(null, new[] { packageId });
            if (!(bool)compilerType.GetProperty("IsValid").GetValue(compiler))
            {
                throw new InvalidOperationException("The selected C# compiler could not be installed or located for " + asmdefPath);
            }
            return new CustomCompilerBuild(core, settings, compiler, asmdefPath);
        }

        internal async Task<CompilerMessage[]> Build(AssemblyBuilder builder, string assemblyName,
            string outputPath, string[] sources, bool releaseBuild)
        {
            string responsePath = Path.GetFullPath(Path.ChangeExtension(outputPath, ".rsp"));
            StringBuilder response = new StringBuilder();
            response.AppendLine("/target:library");
            response.AppendLine("/nostdlib+");
            response.AppendLine("/langversion:7.3");
            response.AppendLine("/debug:portable");
            response.AppendLine(releaseBuild ? "/optimize+" : "/optimize-");
            response.AppendLine(builder.compilerOptions.AllowUnsafeCode ? "/unsafe+" : "/unsafe-");
            response.AppendLine("/out:" + Quote(Path.GetFullPath(outputPath)));
            foreach (string define in builder.defaultDefines.Concat(builder.additionalDefines ?? Array.Empty<string>()).Distinct())
            {
                response.AppendLine("/define:" + define);
            }
            IEnumerable<string> references = builder.defaultReferences
                .Except(builder.excludeReferences ?? Array.Empty<string>())
                .Concat(builder.additionalReferences ?? Array.Empty<string>()).Distinct();
            foreach (string reference in references)
            {
                response.AppendLine("/reference:" + Quote(Path.GetFullPath(reference)));
            }
            // AssemblyBuilder does not consume the asmdef's csc.rsp on Unity 2019.
            string localResponse = Path.Combine(Path.GetDirectoryName(asmdefPath), "csc.rsp");
            if (File.Exists(localResponse))
            {
                response.AppendLine(File.ReadAllText(localResponse));
            }
            foreach (string source in sources)
            {
                response.AppendLine(Quote(Path.GetFullPath(source)));
            }
            string text = (string)coreType.GetMethod("ModifyResponseFile").Invoke(null,
                new[] { settings, assemblyName, asmdefPath, response.ToString() });
            File.WriteAllText(responsePath, text, new UTF8Encoding(false));

            ProcessStartInfo start = new ProcessStartInfo
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            compiler.GetType().GetMethod("Setup").Invoke(compiler,
                new object[] { start, Quote(responsePath), Application.platform });
            using (Process process = new Process { StartInfo = start })
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Could not start the custom C# compiler.");
                }
                Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                string output = await stdout + Environment.NewLine + await stderr;
                File.WriteAllText(Path.ChangeExtension(responsePath, ".compiler.log"), output);
                return ParseMessages(output, process.ExitCode);
            }
        }

        private static string Quote(string value) => "\"" + value.Replace('\\', '/') + "\"";

        private static CompilerMessage[] ParseMessages(string output, int exitCode)
        {
            List<CompilerMessage> messages = new List<CompilerMessage>();
            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Match match = Regex.Match(line,
                    @"^(?:(?<file>.+)\((?<line>\d+),(?<column>\d+)\):\s*)?(?<severity>error|warning)\s+(?<message>.+)$");
                if (!match.Success)
                {
                    continue;
                }
                messages.Add(new CompilerMessage
                {
                    file = match.Groups["file"].Value,
                    line = match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value) : 0,
                    column = match.Groups["column"].Success ? int.Parse(match.Groups["column"].Value) : 0,
                    type = match.Groups["severity"].Value == "error" ? CompilerMessageType.Error : CompilerMessageType.Warning,
                    message = match.Groups["message"].Value
                });
            }
            if (exitCode != 0 && !messages.Any(message => message.type == CompilerMessageType.Error))
            {
                messages.Add(new CompilerMessage { type = CompilerMessageType.Error, message = output, file = string.Empty });
            }
            return messages.ToArray();
        }
    }
}
