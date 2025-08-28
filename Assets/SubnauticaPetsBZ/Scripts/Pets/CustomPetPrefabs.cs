using DaftAppleGames.SubnauticaPets.Pets;
using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Assets;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class CustomPetPrefabs
    {
        internal static void RegisterAll()
        {
            CatPetPrefab.Register();
            DogPetPrefab.Register();
            RabbitPetPrefab.Register();
            SealPetPrefab.Register();
            WalrusPetPrefab.Register();
            FoxPetPrefab.Register();
        }

        // Cat
        internal static class CatPetPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "PetCat";
            private const string PrefabAssetName = "PetCat.prefab";
            private const string IconTextureAssetName = "CatTexture.png";
            private const string AudioAssetName = "CatMeow.wav";
            
            /// <summary>
            /// Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, PetDnaPrefabs.CatDnaPrefab.Info.TechType);
            }
        }

        internal static class DogPetPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "DogPet";
            private const string PrefabAssetName = "PetDog.prefab";
            private const string IconTextureAssetName = "DogTexture.png";
            private const string AudioAssetName = "DogBark.wav";
            
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        internal static class RabbitPetPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "RabbitPet";
            private const string PrefabAssetName = "PetRabbit.prefab";
            private const string IconTextureAssetName = "RabbitTexture.png";
            private const string AudioAssetName = "RabbitSqueak.wav";
            
            /// <summary>
            /// Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        internal static class SealPetPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "SealPet";
            private const string PrefabAssetName = "PetSeal.prefab";
            private const string IconTextureAssetName = "SealTexture.png";
            private const string AudioAssetName = "SealBark.wav";

            /// <summary>
            /// Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        internal static class WalrusPetPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "WalrusPet";
            private const string PrefabAssetName = "PetWalrus.prefab";
            private const string IconTextureAssetName = "WalrusTexture.png";
            private const string AudioAssetName = "WalrusSound.wav";
            
            /// <summary>
            /// Register Walrus
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        // Fox
        internal static class FoxPetPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "FoxPet";
            private const string PrefabAssetName = "PetFox.prefab";
            private const string IconTextureAssetName = "FoxTexture.png";
            private const string AudioAssetName = "FoxSound.wav";
            
            /// <summary>
            /// Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }
    }
}