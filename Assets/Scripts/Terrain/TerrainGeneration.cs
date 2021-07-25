//Adapted from tutorial used previously https://www.youtube.com/watch?v=wbpMiKiSKm8&t=1s
// Covered by MIT License

using System;
using System.Collections;
using System.Collections.Generic;
using Manager_Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class TerrainGeneration : MonoBehaviour
{
    public bool autoUpdate = true;

    [Header("Forest")] public bool generateTrees;
    public GameObject treePrefab;
    public int numberOfTrees;
    public Transform treeParent;

    [Header("Mesh Components")] public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    [Space(5)] public TerrainSettings terrainSettings;
    [Space(5)] public List<Biome> biomes;

    private const int MapChunkSize = 239;
    private int[] _triangles;
    private int _triIndex;
    private int _borderTriIndex;
    private Vector2[] _uvs;
    private Vector3[] _vertices;
    private Vector3[] _borderVertices;
    private int[] _borderTriangles;
    private float[,] map;
    private List<Vector3> _grassTiles = new List<Vector3>();
    private List<Vector3> _foodTiles = new List<Vector3>();
    private List<Vector3> _preyTiles = new List<Vector3>();
    private List<Vector3> _predatorTiles = new List<Vector3>();
    private List<GameObject> _treeList = new List<GameObject>();

    void Awake()
    {
        Generate();
        SetGrassTiles();
    }

    public void Generate()
    {
        map = Heightmap.GenerateHeightmap(terrainSettings.seed, terrainSettings.persistance, terrainSettings.lacunarity,
            terrainSettings.scale, terrainSettings.octaves, terrainSettings.offset, MapChunkSize + 2);
        Color[] colors = new Color[MapChunkSize * MapChunkSize];
        _triIndex = 0;
        _borderTriIndex = 0;

        int meshSize = MapChunkSize - 2;
        int increment = terrainSettings.levelOfDetail == 0 ? 1 : terrainSettings.levelOfDetail * 2;
        int vertPerLine = (meshSize - 1) / increment + 1;

        int[,] indices = new int[MapChunkSize, MapChunkSize];
        int meshIndex = 0;
        int borderIndex = -1;

        _vertices = new Vector3[vertPerLine * vertPerLine];
        _borderVertices = new Vector3[vertPerLine * 4 + 4];
        _triangles = new int[(MapChunkSize - 1) * (MapChunkSize - 1) * 6];
        _borderTriangles = new int[24 * vertPerLine];
        _uvs = new Vector2[vertPerLine * vertPerLine];

        for (int y = 0; y < MapChunkSize; y += increment)
        {
            for (int x = 0; x < MapChunkSize; x += increment)
            {
                bool isBorder = y == 0 || y == MapChunkSize - 1 || x == 0 || x == MapChunkSize - 1;
                if (isBorder)
                {
                    indices[x, y] = borderIndex;
                    borderIndex--;
                }
                else
                {
                    indices[x, y] = meshIndex;
                    meshIndex++;
                }
            }
        }

        for (int y = 0; y < MapChunkSize; y += increment)
        {
            for (int x = 0; x < MapChunkSize; x += increment)
            {
                int index = indices[x, y];
                Vector2 percent = new Vector2((x - increment) / (float) meshSize, (y - increment) / (float) meshSize);
                float height = terrainSettings.curve.Evaluate(map[x, y]) * terrainSettings.mapHeight;
                Vector3 position = new Vector3(((meshSize - 1) / -2f) + percent.x * meshSize, height,
                    ((meshSize - 1) / 2f) - percent.y * meshSize);
                AddVertex(position, percent, index);

                if (x < MapChunkSize - 1 && y < MapChunkSize - 1)
                {
                    int a = indices[x, y];
                    int b = indices[x + increment, y];
                    int c = indices[x, y + increment];
                    int d = indices[x + increment, y + increment];

                    AddTriangle(a, d, c);
                    AddTriangle(d, a, b);
                }

                index++;
            }
        }

        for (int h = 0; h < MapChunkSize; h++)
        {
            for (int w = 0; w < MapChunkSize; w++)
            {
                float height = map[w, h];
                for (int i = 0; i < biomes.Count; i++)
                {
                    if (height <= biomes[i].height)
                    {
                        colors[h * MapChunkSize + w] = biomes[i].biomeColour;
                        break;
                    }
                }
            }
        }

        SetGrassTiles();
        if (generateTrees) SpawnTrees();

        meshRenderer.sharedMaterial.mainTexture = GenerateTexture(colors);
        Mesh sharedMesh = CreateMesh();
        meshFilter.sharedMesh = sharedMesh;
        meshCollider.sharedMesh = sharedMesh;
    }

    Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.uv = _uvs;
        mesh.normals = CalculateNormals();
        return mesh;
    }

    Texture2D GenerateTexture(Color[] colors)
    {
        Texture2D texture = new Texture2D(MapChunkSize, MapChunkSize);
        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] normalArray = new Vector3[_vertices.Length];
        int count = _triangles.Length / 3;
        for (int i = 0; i < count; i++)
        {
            int indexA = _triangles[i * 3];
            int indexB = _triangles[i * 3 + 1];
            int indexC = _triangles[i * 3 + 2];
            Vector3 normal = SurfaceNormal(indexA, indexB, indexC);
            normalArray[indexA] += normal;
            normalArray[indexB] += normal;
            normalArray[indexC] += normal;
        }

        count = _borderTriangles.Length / 3;
        for (int i = 0; i < count; i++)
        {
            int indexA = _borderTriangles[i * 3];
            int indexB = _borderTriangles[i * 3 + 1];
            int indexC = _borderTriangles[i * 3 + 2];
            Vector3 normal = SurfaceNormal(indexA, indexB, indexC);
            if (indexA >= 0) normalArray[indexA] += normal;
            if (indexB >= 0) normalArray[indexB] += normal;
            if (indexC >= 0) normalArray[indexC] += normal;
        }

        for (int i = 0; i < normalArray.Length; i++)
        {
            normalArray[i].Normalize();
        }

        return normalArray;
    }

    Vector3 SurfaceNormal(int a, int b, int c)
    {
        Vector3 pointA = a < 0 ? _borderVertices[-a - 1] : _vertices[a];
        Vector3 pointB = b < 0 ? _borderVertices[-b - 1] : _vertices[b];
        Vector3 pointC = c < 0 ? _borderVertices[-c - 1] : _vertices[c];
        return Vector3.Cross((pointB - pointA), (pointC - pointA)).normalized;
    }

    void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            _borderTriangles[_borderTriIndex] = a;
            _borderTriangles[_borderTriIndex + 1] = b;
            _borderTriangles[_borderTriIndex + 2] = c;
            _borderTriIndex += 3;
        }
        else
        {
            _triangles[_triIndex] = a;
            _triangles[_triIndex + 1] = b;
            _triangles[_triIndex + 2] = c;
            _triIndex += 3;
        }
    }

    void AddVertex(Vector3 position, Vector2 uv, int index)
    {
        if (index < 0)
        {
            _borderVertices[-index - 1] = position;
        }
        else
        {
            _vertices[index] = position;
            _uvs[index] = uv;
        }
    }

    private void SetGrassTiles()
    {
        for (int i = 0; i < _vertices.Length; i++)
        {
            if (_vertices[i].y == 0f)
            {
                _grassTiles.Add(_vertices[i]);
            }
        }
    }

    public void GenerateSpawnPoints(int predatorCount, int foodCount)
    {
        for (int i = 0; i < predatorCount; i++)
        {
            Vector3 tile = _grassTiles[Random.Range(0, _grassTiles.Count)];
            _grassTiles.Remove(tile);
            _predatorTiles.Add(tile);
        }
        
        for (int i = 0; i < foodCount; i++)
        {
            Vector3 tile = _grassTiles[Random.Range(0, _grassTiles.Count)];
            _grassTiles.Remove(tile);
            _foodTiles.Add(tile);
        }
    }

    void SpawnTrees()
    {
        foreach (var tree in _treeList)
        {
            DestroyImmediate(tree.gameObject);
        }
        _treeList.Clear();
        for (int i = 0; i < numberOfTrees; i++)
        {
            Vector3 tile = _grassTiles[Random.Range(0, _grassTiles.Count)];
            var tree = Instantiate(treePrefab, tile, Quaternion.identity, treeParent);
            _grassTiles.Remove(tile);
            _treeList.Add(tree);
        }
    }

    public Vector3 GetRandomGrassTile()
    {
        return _grassTiles[Random.Range(0, _grassTiles.Count)];
    }

    public Vector3 SpawnPopulation(String populationName, int index)
    {
        if (populationName == "Predators")
        {
            return _predatorTiles[index];
        }
        if (populationName == "Prey")
        {
            return _preyTiles[index];
        }
        return _foodTiles[index];
    }

    [Serializable]
    public class Biome
    {
        public string biomeName;
        [Range(0,1)] public float height;
        public Color biomeColour;
        public bool canSpawnFood;
    }

    [Serializable]
    public class TerrainSettings
    {
        public AnimationCurve curve;
        public float lacunarity;
        [Range(0,6)]public int levelOfDetail;
        public float mapHeight;
        public int octaves;
        public Vector2 offset;
        [Range(0,1)]public float persistance;
        public float scale;
        public int seed;
    }
}
