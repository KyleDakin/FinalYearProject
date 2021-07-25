using System.Collections;
using System.Collections.Generic;
using Creature;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Manager_Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Space(5)]
        public GameObject predatorPrefab;
        public int predatorCount;
        [Space(5)]
        public GameObject plant;
        public int foodCount;
        [Space(5)]
        public TimeOfDay timeOfDay;
        public int timeInDay;
        public int generationCount;

        private float _currentTime;
        private TerrainGeneration _terrainGeneration;
        private List<GameObject> plants = new List<GameObject>();
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }

            _terrainGeneration = GetComponent<TerrainGeneration>();
            _terrainGeneration.GenerateSpawnPoints(predatorCount,foodCount);
            NewGeneration();
        }

        public Vector3 GetRandomGrassTile()
        {
            Vector3 tile = _terrainGeneration.GetRandomGrassTile();
            return new Vector3(tile.x, 1, tile.z);
        }

        private void Update()
        {
            _currentTime += Time.deltaTime;

            timeInDay = (timeOfDay == TimeOfDay.Day) ? 50 : 5;
            if (_currentTime >= timeInDay)
            {
                ChangeTimeOfDay();
                _currentTime = 0;
            }
        }

        void NewGeneration()
        {
            timeOfDay = TimeOfDay.Day;
            _currentTime = 0;
            SpawnPredators();
            SpawnFood();
            generationCount++;
        }

        void SpawnPredators()
        {
            for (int i = 0; i < predatorCount; i++)
            {
                Vector3 spawnPos = _terrainGeneration.SpawnPopulation("Predators", i);
                Instantiate(predatorPrefab, spawnPos, Quaternion.identity);
            }
        }

        void SpawnFood()
        {
            foreach (var plant in plants)
            {
                Destroy(plant);
            }
            plants.Clear();
            for (int i = 0; i < foodCount; i++)
            {
                Vector3 spawnPos = _terrainGeneration.SpawnPopulation("Plant", i);
                var plant = Instantiate(this.plant, spawnPos, Quaternion.identity);
                plants.Add(plant.gameObject);
            }
        }

        void ChangeTimeOfDay()
        {
            timeOfDay = timeOfDay == TimeOfDay.Day ? TimeOfDay.Night : TimeOfDay.Day;
            SpawnFood();
        }

        [System.Serializable]
        public enum TimeOfDay
        {
            Day,
            Night
        }
    }
}