using System;
using System.IO;
using System.Threading.Tasks;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Paths;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline)), ManifestProcessor]
    public class DeleteFiles : FlowPipelineJob
    {
        public string[] fileExtensions;
        public string Path;
        public bool IsFatal;

        protected override Task ExecuteInternal(Pipeline pipeline)
        {
            var path = Path.Resolve(pipeline, this);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(path);
            }

            try
            {
                foreach (string extension in fileExtensions)
                {
                    pipeline.Log(LogLevel.Information, $"Removing {extension} files from: {path}");
                    foreach (var file in Directory.EnumerateFiles(path, $"*.{extension}", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception e)
            {
                if (IsFatal)
                {
                    throw e;
                }
            }

            return Task.CompletedTask;
        }
    }
}