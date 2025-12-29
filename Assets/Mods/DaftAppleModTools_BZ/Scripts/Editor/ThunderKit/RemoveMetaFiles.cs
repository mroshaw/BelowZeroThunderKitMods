using System.IO;
using System.Threading.Tasks;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Paths;
using UnityEngine;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline)), ManifestProcessor]
    public class RemoveMetaFiles : FlowPipelineJob
    {
        public string Path;

        protected override Task ExecuteInternal(Pipeline pipeline)
        {
            var path = Path.Resolve(pipeline, this);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(path);
            }
            pipeline.Log(LogLevel.Information, $"Removing meta files from: {path}");
            foreach (var file in Directory.EnumerateFiles(path, "*.meta", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
            
            return Task.CompletedTask;
        }
    }
}