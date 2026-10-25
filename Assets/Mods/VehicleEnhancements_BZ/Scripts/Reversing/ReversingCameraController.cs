using System.Collections.Generic;
using Sirenix.OdinInspector;
using UWE;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Reversing
{
    internal class ReversingCameraController : MonoBehaviour
    {
        private const float LayoutPreviewDuration = 3.0f;

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
        private VehicleHudElementLayout rearViewLayout;
        private Camera configuredRearCamera;
        private RenderTexture originalCameraTargetTexture;
        private int originalCameraCullingMask;
        private bool originalCameraEnabled;
        private WaterscapeVolumeOnCamera configuredWaterscapeEffect;
        private WaterscapeVolumeOnCamera originalWaterscapeEffect;
        private WaterscapeVolume originalWaterscapeSettings;
        private bool originalWaterscapeEnabled;
        private Texture originalRearViewTexture;
        private Image frameImage;
        private Sprite originalFrameSprite;
        private RenderTexture renderTexture;
        private float nextDiagnosticTime;
        private bool wasShowingRearView;
        private Vector3 lastCameraLayoutSettings;
        private float previewExpiresAt;

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
            rearViewLayout = seaTruckHud.rearView.AddComponent<VehicleHudElementLayout>();
            rearViewLayout.Configure(EnhancedVehicle.Seatruck, VehicleHudElementLayout.Element.ReversingCamera);
            lastCameraLayoutSettings = GetCameraLayoutSettings();
            CreateRenderTexture();
        }

        private void ApplyCameraFrameSprite()
        {
            Transform frameTransform = seaTruckHud.rearView.transform.Find(rearViewFramePath);
            frameImage = frameTransform ? frameTransform.GetComponent<Image>() : null;
            if (!frameImage || !SeaTruckCameraFrameSprite)
            {
                ModDebugLog.LogError("Could not replace the vanilla SeaTruck reversing camera frame sprite.");
                return;
            }

            originalFrameSprite = frameImage.sprite;
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
            originalRearViewTexture = rearViewImage.texture;
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
            bool preview = ShouldPreviewRearView();
            bool showRearView = (preview || ConfigFile.EnableReversingCamera) && TryRenderRearView(preview);
            if (!showRearView)
            {
                RestoreRearCamera();
            }
            seaTruckHud.rearView.SetActive(showRearView);
            if (showRearView)
            {
                // The vanilla HUD hides this object in Update, so its own LateUpdate
                // cannot reliably drive layout after we reactivate it here.
                rearViewLayout.ApplyLayout();
            }

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

        private bool ShouldPreviewRearView()
        {
            Vector3 settings = GetCameraLayoutSettings();
            bool changed = settings != lastCameraLayoutSettings;
            lastCameraLayoutSettings = settings;
            if (!FreezeTime.Contains(FreezeTime.Id.IngameMenu))
            {
                previewExpiresAt = 0.0f;
                return false;
            }

            if (changed)
            {
                previewExpiresAt = Time.unscaledTime + LayoutPreviewDuration;
            }

            return Time.unscaledTime < previewExpiresAt;
        }

        private static Vector3 GetCameraLayoutSettings() => new Vector3(
            ConfigFile.SeatruckCameraHorizontalAdjust,
            ConfigFile.SeatruckCameraVerticalAdjust,
            ConfigFile.SeatruckCameraUiScale);

        private void OnDisable()
        {
            RestoreRearCamera();
            seaTruckChain.Clear();
            wasShowingRearView = false;
            previewExpiresAt = 0.0f;

            if (seaTruckHud && seaTruckHud.rearView)
            {
                seaTruckHud.rearView.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            RestoreRearCamera();
            if (rearViewImage && rearViewImage.texture == renderTexture)
            {
                rearViewImage.texture = originalRearViewTexture;
            }

            if (frameImage && frameImage.sprite == SeaTruckCameraFrameSprite)
            {
                frameImage.sprite = originalFrameSprite;
            }

            if (renderTexture)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }

        private bool TryRenderRearView(bool preview)
        {
            SeaTruckMotor seaTruckMotor = GetPilotedSeaTruckMotor();
            if (!seaTruckMotor || (!preview && GetSignedForwardSpeed(seaTruckMotor) >= -reversingSpeedThreshold))
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
                $"viewPosition={seaTruckHud.rearView.transform.localPosition} " +
                $"viewScale={seaTruckHud.rearView.transform.localScale} " +
                $"maskActive={(mask ? mask.isActiveAndEnabled : false)}.");
        }

        private void ConfigureRearCamera(Camera rearCamera)
        {
            if (configuredRearCamera == rearCamera)
            {
                return;
            }

            RestoreRearCamera();
            configuredRearCamera = rearCamera;
            originalCameraTargetTexture = rearCamera.targetTexture;
            originalCameraCullingMask = rearCamera.cullingMask;
            originalCameraEnabled = rearCamera.enabled;
            Camera mainCamera = MainCamera.camera;
            rearCamera.cullingMask = reversingCameraCullingMask;
            rearCamera.targetTexture = renderTexture;
            rearCamera.enabled = false;

            configuredWaterscapeEffect = rearCamera.GetComponent<WaterscapeVolumeOnCamera>();
            originalWaterscapeEffect = configuredWaterscapeEffect;
            if (configuredWaterscapeEffect)
            {
                originalWaterscapeEnabled = configuredWaterscapeEffect.enabled;
                originalWaterscapeSettings = configuredWaterscapeEffect.settings;
            }
            else
            {
                configuredWaterscapeEffect = rearCamera.gameObject.AddComponent<WaterscapeVolumeOnCamera>();
            }

            WaterscapeVolumeOnCamera mainWaterscapeEffect =
                mainCamera ? mainCamera.GetComponent<WaterscapeVolumeOnCamera>() : null;
            bool enableEffect = enableWaterscapeEffects &&
                                mainWaterscapeEffect &&
                                mainWaterscapeEffect.settings;
            configuredWaterscapeEffect.enabled = enableEffect;
            if (enableEffect)
            {
                configuredWaterscapeEffect.settings = mainWaterscapeEffect.settings;
            }
        }

        private void RestoreRearCamera()
        {
            if (!configuredRearCamera)
            {
                return;
            }

            configuredRearCamera.targetTexture = originalCameraTargetTexture;
            configuredRearCamera.cullingMask = originalCameraCullingMask;
            configuredRearCamera.enabled = originalCameraEnabled;
            if (originalWaterscapeEffect)
            {
                originalWaterscapeEffect.settings = originalWaterscapeSettings;
                originalWaterscapeEffect.enabled = originalWaterscapeEnabled;
            }
            else if (configuredWaterscapeEffect)
            {
                Destroy(configuredWaterscapeEffect);
            }

            configuredRearCamera = null;
            configuredWaterscapeEffect = null;
            originalWaterscapeEffect = null;
            originalCameraTargetTexture = null;
            originalWaterscapeSettings = null;
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
