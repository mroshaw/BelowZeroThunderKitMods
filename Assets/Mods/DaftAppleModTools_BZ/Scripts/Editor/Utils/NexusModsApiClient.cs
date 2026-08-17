using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace DaftAppleGames.Editor
{
    public sealed class NexusModsApiClient : IDisposable
    {
        private const string ApiBaseUrl = "https://api.nexusmods.com/v3";
        private const int MaximumUploadPollAttempts = 60;
        private const int UploadPollDelayMilliseconds = 2000;

        private readonly HttpClient apiClient;
        private readonly HttpClient uploadClient;

        /// <summary>
        /// Creates a Nexus Mods v3 API client using the supplied personal API key
        /// </summary>
        public NexusModsApiClient(string apiKey)
        {
            apiClient = new HttpClient();
            apiClient.DefaultRequestHeaders.Add("apikey", apiKey);
            apiClient.DefaultRequestHeaders.Add("User-Agent", "DaftAppleModTools-UnityEditor");
            uploadClient = new HttpClient();
        }

        /// <summary>
        /// Uploads an archive and creates a new version of an existing Nexus Mods file
        /// </summary>
        public async Task<string> UploadNewVersionAsync(
            NexusModsUploadOptions options,
            string zipFilePath,
            string version,
            string changelog,
            IProgress<NexusUploadProgress> progress,
            CancellationToken cancellationToken)
        {
            FileInfo fileInfo = new FileInfo(zipFilePath);
            string globalModId = null;
            if (!string.IsNullOrWhiteSpace(changelog))
            {
                progress.Report(new NexusUploadProgress(0.01f, "Resolving Nexus mod..."));
                globalModId = await ResolveGlobalModIdAsync(
                    options.GameDomain,
                    options.GameScopedModId,
                    cancellationToken);
            }

            progress.Report(new NexusUploadProgress(0.02f, "Creating multipart upload..."));

            JObject createUploadBody = new JObject
            {
                ["filename"] = fileInfo.Name,
                ["size_bytes"] = fileInfo.Length.ToString()
            };
            JObject createUploadResponse = await SendApiRequestAsync(
                HttpMethod.Post,
                "/uploads/multipart",
                createUploadBody,
                cancellationToken);
            JObject uploadData = RequireObject(createUploadResponse, "data");
            string uploadId = RequireString(uploadData, "id");
            JArray partUrls = RequireArray(uploadData, "part_presigned_urls");
            int partSize = RequireInt(uploadData, "part_size_bytes");
            string completeUrl = RequireString(uploadData, "complete_presigned_url");

            List<string> etags = await UploadPartsAsync(
                zipFilePath,
                partUrls,
                partSize,
                progress,
                cancellationToken);

            progress.Report(new NexusUploadProgress(0.72f, "Completing multipart upload..."));
            await CompleteMultipartUploadAsync(completeUrl, etags, cancellationToken);

            progress.Report(new NexusUploadProgress(0.76f, "Finalising upload..."));
            await SendApiRequestAsync(
                HttpMethod.Post,
                $"/uploads/{Uri.EscapeDataString(uploadId)}/finalise",
                null,
                cancellationToken);

            await WaitForUploadAsync(uploadId, progress, cancellationToken);

            progress.Report(new NexusUploadProgress(0.92f, "Creating Nexus file version..."));
            JObject createVersionBody = new JObject
            {
                ["upload_id"] = uploadId,
                ["name"] = string.IsNullOrWhiteSpace(options.DisplayName) ? fileInfo.Name : options.DisplayName,
                ["description"] = NullIfEmpty(options.Description),
                ["version"] = version,
                ["file_category"] = string.IsNullOrWhiteSpace(options.FileCategory) ? "main" : options.FileCategory,
                ["archive_existing_file"] = options.ArchiveExistingVersion,
                ["primary_mod_manager_download"] = options.PrimaryModManagerDownload,
                ["allow_mod_manager_download"] = options.AllowModManagerDownload,
                ["show_requirements_pop_up"] = options.ShowRequirementsPopup,
                ["update_mod_version"] = options.UpdateModVersion
            };
            JObject versionResponse = await SendApiRequestAsync(
                HttpMethod.Post,
                $"/mod-files/{Uri.EscapeDataString(options.FileGroupId)}/versions",
                createVersionBody,
                cancellationToken);
            JObject versionData = RequireObject(versionResponse, "data");
            JObject versionObject = RequireObject(versionData, "version");
            string versionId = RequireString(versionObject, "id");

            if (!string.IsNullOrWhiteSpace(changelog))
            {
                progress.Report(new NexusUploadProgress(0.97f, "Adding changelog..."));
                JObject changelogBody = new JObject
                {
                    ["version"] = version,
                    ["changelog"] = changelog
                };
                await SendApiRequestAsync(
                    HttpMethod.Post,
                    $"/mods/{Uri.EscapeDataString(globalModId)}/changelogs",
                    changelogBody,
                    cancellationToken);
            }

            progress.Report(new NexusUploadProgress(1.0f, "Upload complete."));
            return versionId;
        }

        private async Task<string> ResolveGlobalModIdAsync(
            string gameDomain,
            string gameScopedModId,
            CancellationToken cancellationToken)
        {
            JObject response = await SendApiRequestAsync(
                HttpMethod.Get,
                $"/games/{Uri.EscapeDataString(gameDomain)}/mods/{Uri.EscapeDataString(gameScopedModId)}",
                null,
                cancellationToken);
            JObject data = RequireObject(response, "data");
            return RequireString(data, "id");
        }

        /// <summary>
        /// Releases the HTTP resources used by the Nexus Mods client
        /// </summary>
        public void Dispose()
        {
            apiClient.Dispose();
            uploadClient.Dispose();
        }

        private async Task<List<string>> UploadPartsAsync(
            string filePath,
            JArray partUrls,
            int partSize,
            IProgress<NexusUploadProgress> progress,
            CancellationToken cancellationToken)
        {
            List<string> etags = new List<string>(partUrls.Count);
            byte[] buffer = new byte[partSize];

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                for (int index = 0; index < partUrls.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int bytesRead = await ReadPartAsync(stream, buffer, cancellationToken);
                    ByteArrayContent content = new ByteArrayContent(buffer, 0, bytesRead);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, partUrls[index].Value<string>()))
                    {
                        request.Content = content;
                        using (HttpResponseMessage response = await uploadClient.SendAsync(request, cancellationToken))
                        {
                            await EnsureSuccessAsync(response, "upload file part");
                            string etag = response.Headers.ETag == null
                                ? null
                                : response.Headers.ETag.Tag.Trim('"');
                            if (string.IsNullOrEmpty(etag))
                            {
                                throw new InvalidOperationException($"Nexus upload part {index + 1} did not return an ETag.");
                            }

                            etags.Add(etag);
                        }
                    }

                    float uploadProgress = 0.05f + (0.65f * (index + 1) / partUrls.Count);
                    progress.Report(new NexusUploadProgress(
                        uploadProgress,
                        $"Uploaded part {index + 1} of {partUrls.Count}..."));
                }
            }

            return etags;
        }

        private async Task CompleteMultipartUploadAsync(
            string completeUrl,
            IReadOnlyList<string> etags,
            CancellationToken cancellationToken)
        {
            StringBuilder xmlBuilder = new StringBuilder("<CompleteMultipartUpload>");
            for (int index = 0; index < etags.Count; index++)
            {
                xmlBuilder.Append("<Part><PartNumber>");
                xmlBuilder.Append(index + 1);
                xmlBuilder.Append("</PartNumber><ETag>");
                xmlBuilder.Append(etags[index]);
                xmlBuilder.Append("</ETag></Part>");
            }

            xmlBuilder.Append("</CompleteMultipartUpload>");
            using (StringContent content = new StringContent(xmlBuilder.ToString(), Encoding.UTF8, "application/xml"))
            using (HttpResponseMessage response = await uploadClient.PostAsync(completeUrl, content, cancellationToken))
            {
                await EnsureSuccessAsync(response, "complete multipart upload");
            }
        }

        private async Task WaitForUploadAsync(
            string uploadId,
            IProgress<NexusUploadProgress> progress,
            CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < MaximumUploadPollAttempts; attempt++)
            {
                progress.Report(new NexusUploadProgress(0.80f, "Waiting for Nexus to process the upload..."));
                JObject response = await SendApiRequestAsync(
                    HttpMethod.Get,
                    $"/uploads/{Uri.EscapeDataString(uploadId)}",
                    null,
                    cancellationToken);
                JObject data = RequireObject(response, "data");
                if (RequireString(data, "state") == "available")
                {
                    return;
                }

                await Task.Delay(UploadPollDelayMilliseconds, cancellationToken);
            }

            throw new TimeoutException("Nexus Mods did not finish processing the upload in time.");
        }

        private async Task<JObject> SendApiRequestAsync(
            HttpMethod method,
            string path,
            JObject body,
            CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, ApiBaseUrl + path))
            {
                if (body != null)
                {
                    request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                }

                using (HttpResponseMessage response = await apiClient.SendAsync(request, cancellationToken))
                {
                    await EnsureSuccessAsync(response, $"call {path}");
                    string json = await response.Content.ReadAsStringAsync();
                    return string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
                }
            }
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string responseText = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Failed to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}. {responseText}");
        }

        private static async Task<int> ReadPartAsync(
            Stream stream,
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead = await stream.ReadAsync(
                    buffer,
                    totalBytesRead,
                    buffer.Length - totalBytesRead,
                    cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        private static JToken NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? JValue.CreateNull() : new JValue(value);

        private static JObject RequireObject(JObject parent, string propertyName)
        {
            JObject value = parent[propertyName] as JObject;
            if (value == null)
            {
                throw new InvalidOperationException($"Nexus response did not contain '{propertyName}'.");
            }

            return value;
        }

        private static JArray RequireArray(JObject parent, string propertyName)
        {
            JArray value = parent[propertyName] as JArray;
            if (value == null)
            {
                throw new InvalidOperationException($"Nexus response did not contain '{propertyName}'.");
            }

            return value;
        }

        private static string RequireString(JObject parent, string propertyName)
        {
            string value = parent.Value<string>(propertyName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Nexus response did not contain '{propertyName}'.");
            }

            return value;
        }

        private static int RequireInt(JObject parent, string propertyName)
        {
            int? value = parent.Value<int?>(propertyName);
            if (!value.HasValue || value.Value <= 0)
            {
                throw new InvalidOperationException($"Nexus response did not contain a valid '{propertyName}'.");
            }

            return value.Value;
        }
    }
}
