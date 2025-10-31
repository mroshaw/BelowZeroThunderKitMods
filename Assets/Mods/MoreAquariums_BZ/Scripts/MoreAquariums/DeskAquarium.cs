using System.Collections.Generic;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    public class DeskAquarium : AquariumBase
    {
        // Non-static properties of the aquarium component
        public override int StorageHeight => 1;
        public override int StorageWidth => 2;
        
        // Properties of the Aquarium
        private static readonly PrefabData Data = new PrefabData
        {
            ClassId = "DeskAquarium",
            DisplayName = "Desk Aquarium",
            Description = "A miniature aquarium to place on a desk, table or shelf.",
            IconAssetName = "DeskAquariumIcon.png",
            PrefabAssetName = "DeskAquariumPrefab.prefab",
            AquariumType = AquariumType.Desk,
            StorageHeight = 1,
            StorageWidth = 2,
            AllowConstructionOnConstructables = true,
            UseCustomMovement = true,
            WaveScale = 0.9f,
            PostConfigAction = PostConfigAction,
            ReplaceModel = false,
            AddBubbleAudio = false,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 1),
                new Ingredient(TechType.Glass, 1)),
        };

        /// <summary>
        /// Perform aquarium type specific post configuration of the new prefab
        /// </summary>
        internal static void PostConfigAction(GameObject newPrefabGo)
        {
            ModDebugLog.LogError("Running PostConfigAction for DeskAquarium...");
            Transform mainBubblesTransform = newPrefabGo.transform.Find("Bubbles/xBubbles");
            if (!mainBubblesTransform)
            {
                ModDebugLog.LogError("Bubbles transform not found!");
                return;
            }

            // Grab all the bubbles particle systems
            // Get the main one
            ParticleSystem mainBubbles = mainBubblesTransform.GetComponent<ParticleSystem>();

            // Find the two laterBubbles systems
            List<ParticleSystem> lateralBubbleSystems = new List<ParticleSystem>();
            foreach (Transform childTransform in mainBubbles.GetComponentsInChildren<Transform>())
            {
                if (childTransform.gameObject.name == "xLateralBubbles")
                {
                    lateralBubbleSystems.Add(childTransform.gameObject.GetComponent<ParticleSystem>());
                }
            }
            if (lateralBubbleSystems.Count == 0)
            {
                ModDebugLog.LogError("xLateralBubbles transforms not found!");
                return;
            }
            
            // Find the dotsBubbles system
            Transform dotsBubblesTransform = mainBubblesTransform.Find("xDots");
            if (!dotsBubblesTransform)
            {
                ModDebugLog.LogError("xDots transform not found!");
                return;
            }
            ParticleSystem dotsBubbles = dotsBubblesTransform.gameObject.GetComponent<ParticleSystem>();

            // Configure each ParticleSystem
            ConfigureMainBubbles(mainBubbles);
            foreach (ParticleSystem lateralBubbleSystem in lateralBubbleSystems)
            {
                ConfigureLateralBubbles(lateralBubbleSystem);
            }
            ConfigureDotsBubbles(dotsBubbles);
        }

        /// <summary>
        /// Configure the "Main" central bubbles
        /// </summary>
        private static void ConfigureMainBubbles(ParticleSystem mainParticleSystem)
        {
            // Configure main bubbles
            ModDebugLog.LogDebug("Configuring mainBubbles: Main Module");
            ParticleSystem.MainModule bubblesMain = mainParticleSystem.main;
            ConfigureMain(bubblesMain, 0.7f, 0.9f, 0.02f, 0.06f, bubblesMain.startSpeed.constantMin, bubblesMain.startSpeed.constantMax);
            
            ModDebugLog.LogDebug("Configuring mainBubbles: Shape Module");
            ParticleSystem.ShapeModule bubbleShape = mainParticleSystem.shape;
            ConfigureShape(bubbleShape, 0.2f, 0.04f, 0.0f);
            
            ModDebugLog.LogDebug("Configuring mainBubbles: VelocityOverLifetime Module");
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = mainParticleSystem.velocityOverLifetime;
            ConfigureVelocityOverLifetime(velocityOverLifetime, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,0.3f);
            
            ModDebugLog.LogDebug("Configuring mainBubbles: SizeOverLifetime Module");
            ParticleSystem.SizeOverLifetimeModule bubbleSizeOverLifetime = mainParticleSystem.sizeOverLifetime;
            ConfigureSizeOverLifetime(bubbleSizeOverLifetime, 0.1f, 0.2f);
        }

        /// <summary>
        /// Configure the two "Lateral" bubbles systems
        /// </summary>
        private static void ConfigureLateralBubbles(ParticleSystem lateralBubbles)
        {
            // Configure lateral bubbles
            ModDebugLog.LogDebug("Configuring lateralBubbles: Main Module");
            ParticleSystem.MainModule bubblesMain = lateralBubbles.main;
            ConfigureMain(bubblesMain, 0.7f, 0.9f, 0.02f, 0.06f, 0.2f, 0.3f);
            
            ModDebugLog.LogDebug("Configuring lateralBubbles: Shape Module");
            ParticleSystem.ShapeModule bubbleShape = lateralBubbles.shape;
            // ConfigureShape(bubbleShape, 0.2f, 0.04f, 0.0f);
            
            ModDebugLog.LogDebug("Configuring lateralBubbles: SizeOverLifetime Module");
            ParticleSystem.SizeOverLifetimeModule bubbleSizeOverLifetime = lateralBubbles.sizeOverLifetime;
            ConfigureSizeOverLifetime(bubbleSizeOverLifetime, 0.25f, 0.25f);
        }

        /// <summary>
        /// Configure the "Dots" bubbles
        /// </summary>
        private static void ConfigureDotsBubbles(ParticleSystem dotsBubbles)
        {
            // Configure dots bubbles
            ModDebugLog.LogDebug("Configuring dotsBubbles: Main Module");
            ParticleSystem.MainModule bubblesMain = dotsBubbles.main;
            ConfigureMain(bubblesMain, 0.7f, 0.9f, 0.02f, 0.06f, bubblesMain.startSpeed.constantMin, bubblesMain.startSpeed.constantMax);
            
            ModDebugLog.LogDebug("Configuring dotsBubbles: Shape Module");
            ParticleSystem.ShapeModule bubbleShape = dotsBubbles.shape;
            ConfigureShape(bubbleShape, 0.2f, 0.04f, 0.0f);
            
            ModDebugLog.LogDebug("Configuring dotsBubbles: VelocityOverLifetime Module");
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = dotsBubbles.velocityOverLifetime;
            ConfigureVelocityOverLifetime(velocityOverLifetime, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,0.3f);
            
            ModDebugLog.LogDebug("Configuring dotsBubbles: SizeOverLifetime Module");
            ParticleSystem.SizeOverLifetimeModule bubbleSizeOverLifetime = dotsBubbles.sizeOverLifetime;
            ConfigureSizeOverLifetime(bubbleSizeOverLifetime, 0.05f, 0.1f);
        }
        
        /// <summary>
        /// Configures the "Main" module of the ParticleSystem
        /// </summary>
        private static void ConfigureMain(ParticleSystem.MainModule mainModule, float lifeTimeSizeStart, float lifeTimeSizeEnd,
            float startSizeStart, float startSizeEnd, float startSpeedStart, float startSpeedEnd)
        {
            ParticleSystem.MinMaxCurve lifeTimeSize =  new ParticleSystem.MinMaxCurve(lifeTimeSizeStart, lifeTimeSizeEnd);
            ParticleSystem.MinMaxCurve startSize =  new ParticleSystem.MinMaxCurve(startSizeStart, startSizeEnd);
            ParticleSystem.MinMaxCurve startSpeed =  new ParticleSystem.MinMaxCurve(startSpeedStart, startSpeedEnd);
            mainModule.startLifetime = lifeTimeSize;
            mainModule.startSize = startSize;
            mainModule.startSpeed = startSpeed;
        }

        /// <summary>
        /// Configures the "Shape" module of the ParticleSystem
        /// </summary>
        private static void ConfigureShape(ParticleSystem.ShapeModule shapeModule, float scaleX, float scaleY, float scaleZ)
        {
            Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);
            shapeModule.scale = scale;
        }

        /// <summary>
        /// Configures the "SizeOverLifetime" module of the ParticleSystem
        /// </summary>
        private static void ConfigureSizeOverLifetime(ParticleSystem.SizeOverLifetimeModule sizeOverLifetimeModule,
            float sizeMin, float sizeMax)
        {
            ParticleSystem.MinMaxCurve  sizeOverLifetime = new ParticleSystem.MinMaxCurve {mode = ParticleSystemCurveMode.TwoConstants, constantMin  = sizeMin,  constantMax = sizeMax};
            sizeOverLifetimeModule.size = sizeOverLifetime;
        }
        
        /// <summary>
        /// Configures the "VelocityOverLifetime" module of the ParticleSystem
        /// </summary>
        private static void ConfigureVelocityOverLifetime(ParticleSystem.VelocityOverLifetimeModule velocityOverLifetimeModule, float xLow, float xHigh, float yLow, float yHigh, float zLow, float zHigh)
        {
            ParticleSystem.MinMaxCurve linearX = new ParticleSystem.MinMaxCurve
            {
                mode = ParticleSystemCurveMode.TwoConstants,
                constantMin = xLow,
                constantMax = xHigh
            };
            
            ParticleSystem.MinMaxCurve linearY = new ParticleSystem.MinMaxCurve
            {
                mode = ParticleSystemCurveMode.TwoConstants,
                constantMin = yLow,
                constantMax = yHigh
            };

            
            ParticleSystem.MinMaxCurve linearZ = new ParticleSystem.MinMaxCurve
            {
                mode = ParticleSystemCurveMode.TwoConstants,
                constantMin = zLow,
                constantMax = zHigh
            };
            
            velocityOverLifetimeModule.x = linearX;
            velocityOverLifetimeModule.y = linearY;
            velocityOverLifetimeModule.z = linearZ;
        }
        
        // Register the prefab
        public static void Register() => RegisterInternal(Data);
    }
}