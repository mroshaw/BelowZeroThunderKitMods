using System.Collections;
using System.Collections.Generic;
using DaftAppleGames.SubnauticaPets.Pets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Core.Testing
{
    public class TestSpawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> gameObjectsToEnable;
        [SerializeField] private float delayBeforeSpawn = 2.0f;
        
        private void Awake()
        {
            foreach (GameObject go in gameObjectsToEnable)
            {
                go.SetActive(false);
            }
        }

        private void Start()
        {
            StartCoroutine(SpawnAfterDelay());
        }
        
        private void SpawnAll()
        {
            foreach (GameObject go in gameObjectsToEnable)
            {
                PetPrefabConfigUtils.ConfigureCreature(go);
                go.SetActive(true);
            }
        }

        private IEnumerator SpawnAfterDelay()
        {
            yield return new WaitForSeconds(delayBeforeSpawn);
            SpawnAll();
        }
    }
}