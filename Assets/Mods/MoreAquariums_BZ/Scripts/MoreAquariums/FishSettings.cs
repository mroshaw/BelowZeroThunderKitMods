using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    [CreateAssetMenu(fileName = "FishSettings", menuName = "Mods/More Aquariums/Fish Settings", order = 1)]
    public class FishSettings : ScriptableObject
    {
        [Header("Movement Settings")]
        [SerializeField] internal float baseSpeed = 0.3f;
        [SerializeField] internal float randomSpeedModifier = 0.1f;
        [SerializeField] internal float turnSpeed = 3.0f;
        [SerializeField] internal float arrivalDistance = 0.2f;
        [SerializeField] internal bool stopRoll;
        [SerializeField] internal bool limitPitch = true;
        [SerializeField] internal float pitchScaling = 60.0f;
        [SerializeField] internal float minPitch = -45.0f;
        [SerializeField] internal float maxPitch = 45.0f;
        
        [Header("Darting Behaviour")]
        [SerializeField] internal bool randomDarting = true;
        [SerializeField] internal float dartSpeedMultiplier = 1.4f;
        [SerializeField] internal float dartDuration = 0.5f;
        [SerializeField] internal float dartIntervalMin = 20f;
        [SerializeField] internal float dartIntervalMax = 60f;

        [Header("Fish Avoidance")]
        [SerializeField] internal bool avoidOtherFish;
        [SerializeField] internal float avoidanceRadius = 0.1f;
        [SerializeField] internal float avoidanceStrength = 2f;

        [Header("Noise Motion")]
        [SerializeField] internal bool applyNoise = true;
        [SerializeField] internal float noiseInfluence = 0.5f;
        [SerializeField] internal float noiseSpeed = 0.2f;

        [Header("Bounds Containment")]
        [SerializeField] internal bool clampToBounds = true;
        [SerializeField] internal bool steerFromBounds = true;
        [SerializeField] internal float boundaryMargin = 0.05f;
        [SerializeField] internal float boundarySteerStrength = 0.5f;
        
        [Header("Culling")]
        [SerializeField] internal bool culling = true;
        [SerializeField] internal float cullingDistanceFromPlayer = 20.0f;
    }
}