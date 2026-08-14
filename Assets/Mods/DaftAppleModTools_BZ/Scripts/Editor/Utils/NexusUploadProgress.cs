namespace DaftAppleGames.Editor
{
    public struct NexusUploadProgress
    {
        public float Progress { get; }
        public string Status { get; }

        /// <summary>
        /// Creates an immutable Nexus upload progress update
        /// </summary>
        public NexusUploadProgress(float progress, string status)
        {
            Progress = progress;
            Status = status;
        }
    }
}
