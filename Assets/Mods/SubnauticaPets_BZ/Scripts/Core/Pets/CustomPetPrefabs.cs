using Nautilus.Assets;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

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
            private const string ClassId = "CatPet";
            private const string PrefabAssetName = "PetCat.prefab";
            private const string IconTextureAssetName = "CatIcon_Small.png";
            private const string AudioAssetName = "CatMeow.wav";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, PetDnaPrefabs.CatDnaPrefab.Info.TechType);
            }
        }

        internal static class DogPetPrefab
        {
            private const string ClassId = "DogPet";
            private const string PrefabAssetName = "PetDog.prefab";
            private const string IconTextureAssetName = "DogIcon_Small.png";
            private const string AudioAssetName = "DogBark.wav";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        internal static class RabbitPetPrefab
        {
            private const string ClassId = "RabbitPet";
            private const string PrefabAssetName = "PetRabbit.prefab";
            private const string IconTextureAssetName = "RabbitIcon_Small.png";
            private const string AudioAssetName = "RabbitSqueak.wav";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        internal static class SealPetPrefab
        {
            private const string ClassId = "SealPet";
            private const string PrefabAssetName = "PetSeal.prefab";
            private const string IconTextureAssetName = "SealIcon_Small.png";
            private const string AudioAssetName = "SealBark.wav";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        internal static class WalrusPetPrefab
        {
            private const string ClassId = "WalrusPet";
            private const string PrefabAssetName = "PetWalrus.prefab";
            private const string IconTextureAssetName = "WalrusIcon_Small.png";
            private const string AudioAssetName = "WalrusSound.wav";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Register Walrus
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }

        // Fox
        internal static class FoxPetPrefab
        {
            private const string ClassId = "FoxPet";
            private const string PrefabAssetName = "PetFox.prefab";
            private const string IconTextureAssetName = "FoxIcon_Small.png";
            private const string AudioAssetName = "FoxSound.wav";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Register Cat
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
                PetPrefabConfigUtils.RegisterCustomPet(Info, ClassId, PrefabAssetName,
                    AudioAssetName,
                    Info.TechType, TechType.None);
            }
        }
    }
}