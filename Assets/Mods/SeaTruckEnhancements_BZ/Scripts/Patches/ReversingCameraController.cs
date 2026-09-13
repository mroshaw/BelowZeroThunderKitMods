using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.SeaTruckEnhancements_BZ.SeaTruckEnhancementsPlugin_BZ;

namespace DaftAppleGames.SeaTruckEnhancements_BZ.UI
{
    internal class ReversingCameraController : MonoBehaviour
    {
        [SerializeField, MinValue(0.0f)]
        private float reversingSpeedThreshold = 0.1f;

        [SerializeField]
        private bool copyMainCameraCullingMask = true;

        [SerializeField]
        private bool enableWaterscapeEffects = true;

        [SerializeField]
        private string rearViewFramePath = "Frame";

        private readonly List<SeaTruckSegment> seaTruckChain = new List<SeaTruckSegment>();
        private uGUI_SeaTruckHUD seaTruckHud;
        private Camera configuredRearCamera;

        private void Awake()
        {
            seaTruckHud = GetComponentInParent<uGUI_SeaTruckHUD>();
            if (!seaTruckHud || !seaTruckHud.rearView)
            {
                ModDebugLog.LogError("Could not find the vanilla SeaTruck reversing camera UI.");
                enabled = false;
                return;
            }

            ApplyCameraFrameSprite();
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

        private void LateUpdate()
        {
            bool showRearView = ConfigFile.EnableReversingCamera && TryRenderRearView();
            seaTruckHud.rearView.SetActive(showRearView);
        }

        private void OnDisable()
        {
            seaTruckChain.Clear();

            if (seaTruckHud && seaTruckHud.rearView)
            {
                seaTruckHud.rearView.SetActive(false);
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

            ConfigureRearCamera(rearCamera);
            rearCamera.Render();
            seaTruckChain.Clear();
            return true;
        }

        private void ConfigureRearCamera(Camera rearCamera)
        {
            if (configuredRearCamera == rearCamera)
            {
                return;
            }

            configuredRearCamera = rearCamera;
            Camera mainCamera = MainCamera.camera;
            if (copyMainCameraCullingMask && mainCamera)
            {
                rearCamera.cullingMask = mainCamera.cullingMask;
            }

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
