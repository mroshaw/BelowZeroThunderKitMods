using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Reversing
{
    internal class ReversingCameraController : MonoBehaviour
    {
        [SerializeField, MinValue(0.0f)]
        private float reversingSpeedThreshold = 0.1f;

        [SerializeField]
        private LayerMask reversingCameraCullingMask = 727758871;

        [SerializeField]
        private bool enableWaterscapeEffects = true;

        [SerializeField]
        private string rearViewFramePath = "Frame";

        [SerializeField]
        private string rearViewImagePath = "Mask/Camera";

        [SerializeField, MinValue(1)]
        private int renderTextureWidth = 512;

        [SerializeField, MinValue(1)]
        private int renderTextureHeight = 512;

        private readonly List<SeaTruckSegment> seaTruckChain = new List<SeaTruckSegment>();
        private uGUI_SeaTruckHUD seaTruckHud;
        private RawImage rearViewImage;
        private Camera configuredRearCamera;
        private RenderTexture renderTexture;
        private Texture2D diagnosticPixel;
        private float nextDiagnosticTime;
        private bool wasShowingRearView;
        private bool diagnosticReadbackFailed;

        private void Awake()
        {
            if (!enabled)
            {
                return;
            }

            seaTruckHud = GetComponentInParent<uGUI_SeaTruckHUD>();
            if (!seaTruckHud || !seaTruckHud.rearView)
            {
                ModDebugLog.LogError("Could not find the vanilla SeaTruck reversing camera UI.");
                enabled = false;
                return;
            }

            ApplyCameraFrameSprite();
            CreateRenderTexture();
        }

        private void ApplyCameraFrameSprite()
        {
            Transform frameTransform = seaTruckHud.rearView.transform.Find(rearViewFramePath);
            Image frameImage = frameTransform ? frameTransform.GetComponent<Image>() : null;
            if (!frameImage || !SeaTruckCameraFrameSprite)
            {
                ModDebugLog.LogError("Could not replace the vanilla SeaTruck reversing camera frame sprite.");
                return;
            }

            frameImage.sprite = SeaTruckCameraFrameSprite;
        }

        private void CreateRenderTexture()
        {
            Transform imageTransform = seaTruckHud.rearView.transform.Find(rearViewImagePath);
            rearViewImage = imageTransform ? imageTransform.GetComponent<RawImage>() : null;
            if (!rearViewImage)
            {
                ModDebugLog.LogError("Could not find the vanilla SeaTruck reversing camera image.");
                enabled = false;
                return;
            }

            renderTexture = new RenderTexture(
                renderTextureWidth,
                renderTextureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = $"SeaTruckReversingCamera_{GetInstanceID()}",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            rearViewImage.texture = renderTexture;
            if (DetailedLoggingEnabled)
            {
                ModDebugLog.LogDebug($"[ReversingCamera] Initialized: RT={renderTexture.name} " +
                                     $"created={renderTexture.IsCreated()} size={renderTexture.width}x{renderTexture.height} " +
                                     $"image={rearViewImage.name} imageActive={rearViewImage.gameObject.activeInHierarchy}.");
            }
        }

        private void LateUpdate()
        {
            bool showRearView = ConfigFile.EnableReversingCamera && TryRenderRearView();
            seaTruckHud.rearView.SetActive(showRearView);
            if (DetailedLoggingEnabled && showRearView &&
                (!wasShowingRearView || Time.unscaledTime >= nextDiagnosticTime))
            {
                LogRenderDiagnostics(configuredRearCamera);
                nextDiagnosticTime = Time.unscaledTime + 2.0f;
            }

            if (DetailedLoggingEnabled && wasShowingRearView && !showRearView)
            {
                ModDebugLog.LogDebug($"[ReversingCamera] Hidden: enabledInConfig={ConfigFile.EnableReversingCamera}.");
            }

            wasShowingRearView = showRearView;
        }

        private void OnDisable()
        {
            seaTruckChain.Clear();
            wasShowingRearView = false;

            if (seaTruckHud && seaTruckHud.rearView)
            {
                seaTruckHud.rearView.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (renderTexture)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (diagnosticPixel)
            {
                Destroy(diagnosticPixel);
                diagnosticPixel = null;
            }
        }

        private bool TryRenderRearView()
        {
            SeaTruckMotor seaTruckMotor = GetPilotedSeaTruckMotor();
            if (!seaTruckMotor || GetSignedForwardSpeed(seaTruckMotor) >= -reversingSpeedThreshold)
            {
                return false;
            }

            SeaTruckSegment mainCab = seaTruckMotor.GetComponent<SeaTruckSegment>();
            if (!mainCab || !mainCab.isMainCab)
            {
                return false;
            }

            seaTruckChain.Clear();
            mainCab.GetTruckChain(seaTruckChain);
            if (seaTruckChain.Count == 0)
            {
                return false;
            }

            Camera rearCamera = seaTruckChain[seaTruckChain.Count - 1].rearCamera;
            if (!rearCamera)
            {
                seaTruckChain.Clear();
                return false;
            }

            if (!renderTexture || !renderTexture.IsCreated())
            {
                seaTruckChain.Clear();
                return false;
            }

            ConfigureRearCamera(rearCamera);
            rearCamera.Render();
            seaTruckChain.Clear();
            return true;
        }

        private void LogRenderDiagnostics(Camera rearCamera)
        {
            Mask mask = rearViewImage.GetComponentInParent<Mask>();
            WaterscapeVolumeOnCamera waterscapeEffect = rearCamera.GetComponent<WaterscapeVolumeOnCamera>();
            ModDebugLog.LogDebug(
                $"[ReversingCamera] Render: camera={rearCamera.name} id={rearCamera.GetInstanceID()} " +
                $"cameraActive={rearCamera.gameObject.activeInHierarchy} cameraEnabled={rearCamera.enabled} " +
                $"targetMatches={rearCamera.targetTexture == renderTexture} " +
                $"cullingMask={rearCamera.cullingMask} clearFlags={rearCamera.clearFlags} " +
                $"waterscapeEnabled={(waterscapeEffect ? waterscapeEffect.enabled : false)} " +
                $"waterscapeSettings={(waterscapeEffect && waterscapeEffect.settings ? true : false)} " +
                $"RTcreated={renderTexture.IsCreated()} imageMatches={rearViewImage.texture == renderTexture} " +
                $"imageEnabled={rearViewImage.enabled} imageActive={rearViewImage.gameObject.activeInHierarchy} " +
                $"imageCull={rearViewImage.canvasRenderer.cull} imageAlpha={rearViewImage.color.a:F2} " +
                $"imageSize={rearViewImage.rectTransform.rect.size} " +
                $"maskActive={(mask ? mask.isActiveAndEnabled : false)}.");

            if (diagnosticReadbackFailed)
            {
                return;
            }

            RenderTexture sampleTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                if (!diagnosticPixel)
                {
                    diagnosticPixel = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                }

                sampleTexture = RenderTexture.GetTemporary(4, 4, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(renderTexture, sampleTexture);
                RenderTexture.active = sampleTexture;
                diagnosticPixel.ReadPixels(new Rect(0, 0, 4, 4), 0, 0, false);
                diagnosticPixel.Apply(false, false);
                ModDebugLog.LogDebug(
                    $"[ReversingCamera] Pixels: center={diagnosticPixel.GetPixel(1, 1)} " +
                    $"UL={diagnosticPixel.GetPixel(0, 3)} UR={diagnosticPixel.GetPixel(3, 3)} " +
                    $"LL={diagnosticPixel.GetPixel(0, 0)} LR={diagnosticPixel.GetPixel(3, 0)}.");
            }
            catch (Exception exception)
            {
                diagnosticReadbackFailed = true;
                ModDebugLog.LogError($"[ReversingCamera] Pixel readback failed: {exception}");
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (sampleTexture)
                {
                    RenderTexture.ReleaseTemporary(sampleTexture);
                }
            }
        }

        private void ConfigureRearCamera(Camera rearCamera)
        {
            if (configuredRearCamera == rearCamera)
            {
                return;
            }

            configuredRearCamera = rearCamera;
            Camera mainCamera = MainCamera.camera;
            rearCamera.cullingMask = reversingCameraCullingMask;
            rearCamera.targetTexture = renderTexture;

            WaterscapeVolumeOnCamera waterscapeEffect =
                rearCamera.GetComponent<WaterscapeVolumeOnCamera>();
            if (!waterscapeEffect)
            {
                waterscapeEffect = rearCamera.gameObject.AddComponent<WaterscapeVolumeOnCamera>();
            }

            WaterscapeVolumeOnCamera mainWaterscapeEffect =
                mainCamera ? mainCamera.GetComponent<WaterscapeVolumeOnCamera>() : null;
            bool enableEffect = enableWaterscapeEffects &&
                                mainWaterscapeEffect &&
                                mainWaterscapeEffect.settings;
            waterscapeEffect.enabled = enableEffect;
            if (enableEffect)
            {
                waterscapeEffect.settings = mainWaterscapeEffect.settings;
            }
        }

        private static SeaTruckMotor GetPilotedSeaTruckMotor()
        {
            Player player = Player.main;
            if (!player)
            {
                return null;
            }

            SeaTruckMotor seaTruckMotor = player.GetComponentInParent<SeaTruckMotor>();
            return seaTruckMotor && seaTruckMotor.IsPiloted() ? seaTruckMotor : null;
        }

        private static float GetSignedForwardSpeed(SeaTruckMotor seaTruckMotor)
        {
            if (!seaTruckMotor.useRigidbody)
            {
                return 0.0f;
            }

            return Vector3.Dot(seaTruckMotor.useRigidbody.velocity, seaTruckMotor.transform.forward);
        }
    }
}
