using System.Collections.Generic;   // List<> and Dictionary<>
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;                  // PrefabUtility + Undo + AssetDatabase. Editor-only, so it's wrapped in #if
#endif

/// <summary>
/// Builds a forest level out of tile prefabs, then scatters trees/rocks/mushrooms on top.
/// Runs in EDIT MODE (button in the Inspector) so the result is a normal, saved scene hierarchy.
///
/// Pipeline:  1) decide what each grid cell is (Meadow / Lake / Path)
///            2) spawn the ground tile for each cell (pivot-corrected, slightly overlapping)
///            3) add an invisible walkable floor under the non-lake cells (no falling through seams)
///            4) scatter decoration layers on the ground using noise + spacing rules
///
/// QUICK START: click "Setup Forest Preset" in the Inspector. It finds your PP_ prefabs and fills in
/// all the layer numbers. Then click Generate and tweak from there.
/// </summary>
public class ForestGenerator : MonoBehaviour
{
    // TODO: Spline-based paths (the straight tiles have no corner pieces yet, so wiggly paths can look broken).

    // ------------------------------------------------------------------
    // TYPES
    // ------------------------------------------------------------------

    enum CellType { Meadow, Lake, Path }

    [System.Serializable]
    public class ScatterLayer
    {
        public string name = "Layer";
        public GameObject[] prefabs;

        [Tooltip("How many random spots to TRY. Not how many get placed: rejected spots are skipped.")]
        [Min(0)] public int attempts = 1000;

        [Tooltip("Personal space. Two objects are rejected if their distance is less than the sum of their radii. 0 = no spacing rule at all (use for grass).")]
        [Min(0f)] public float radius = 1.5f;

        [Tooltip("Size of noise blobs. SMALLER = bigger, smoother clumps. LARGER = noisy and speckled.")]
        public float noiseScale = 0.05f;

        [Tooltip("Spots where noise is below this are rejected. 0 = no noise filtering, 0.7 = only the densest clumps.")]
        [Range(0f, 1f)] public float noiseThreshold = 0.4f;

        [Tooltip("Random size multiplier (min, max).")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.3f);

        [Tooltip("Push up (+) or sink (-) into the ground. Handy for rocks that should look half-buried.")]
        public float yOffset = 0f;

        [Tooltip("Random lean in degrees on X and Z. Good for rocks and mushrooms, keep at 0 for trees.")]
        [Range(0f, 30f)] public float randomTilt = 0f;

        [Tooltip("Rotate to match the slope of the ground under it.")]
        public bool alignToGround = false;

        public bool onMeadow = true;
        public bool onLake = false;
        public bool onPath = false;
    }

    // ------------------------------------------------------------------
    // SETTINGS
    // ------------------------------------------------------------------

    [Header("Seed")]
    [Tooltip("Same seed + same settings = identical forest. Change it for a different forest.")]
    public int seed = 1234;

    [Header("Grid")]
    [Min(1)] public int gridWidth = 8;
    [Min(1)] public int gridHeight = 8;
    [Tooltip("World size of ONE tile. Leave at 0 to auto-measure from the first Meadow prefab (it logs what it found).")]
    public float tileSize = 0f;

    [Header("Ground tiles")]
    [Tooltip("Pulls tiles together by this fraction so neighbours overlap and hide seams. 0 = edge to edge, 0.03 = 3% overlap. Raise it if you still see gaps.")]
    [Range(0f, 0.15f)] public float tileOverlap = 0.03f;
    [Tooltip("Shifts each tile so its VISUAL centre sits on its grid cell, even if the prefab's pivot is at a corner. Untick if your tiles already have centred pivots and look off.")]
    public bool compensatePivot = true;

    [Header("Walkable floor")]
    [Tooltip("Adds an invisible collider under every non-lake cell so the player can't fall through seams or gaps.")]
    public bool addWalkableFloor = true;
    [Tooltip("How far BELOW the lowest ground surface the floor sits. Keep small so it never pokes through the visible ground.")]
    [Min(0f)] public float floorDrop = 0.05f;
    [Tooltip("Thickness of the floor collider.")]
    [Min(0.1f)] public float floorThickness = 2f;

    [Header("Ground prefabs (drag from the PROJECT window, not the Hierarchy)")]
    public GameObject[] meadowPrefabs;                      // PP_Meadow
    public GameObject[] lakePrefabs;                        // PP_Lake_Ground
    public GameObject[] pathPrefabs;                        // PP_Meadow_Path

    [Header("Ground rules")]
    [Tooltip("Noise scale for lakes, measured per TILE. Smaller = fewer, bigger lakes.")]
    public float lakeNoiseScale = 0.25f;
    [Tooltip("Cells with noise below this become lake. 0 = no lakes. ~0.3 = a few lakes. ~0.5 = mostly lake.")]
    [Range(0f, 1f)] public float lakeThreshold = 0.3f;
    [Tooltip("Randomly rotate meadow/lake tiles in 90-degree steps for variety. Turn off if tile edges don't match when rotated.")]
    public bool randomRotateGround = true;

    [Header("Paths")]
    [Tooltip("How many paths to carve. They alternate: West->East, North->South, West->East... so they cross and connect.")]
    [Min(0)] public int pathCount = 2;
    [Tooltip("Chance per step that the path sidesteps one tile. 0 = dead straight (cleanest joins), 0.5 = very wiggly.")]
    [Range(0f, 1f)] public float pathWander = 0.25f;
    [Tooltip("Tick if the path on your path tile runs along world X when the tile has rotation 0.")]
    public bool pathRunsAlongX = true;

    [Header("Decoration")]
    [Tooltip("Master dial. Multiplies every layer's Attempts. 0.5 = half as much stuff, 2 = twice as much.")]
    [Min(0f)] public float densityMultiplier = 1f;
    [Tooltip("Order matters: earlier layers claim space first.")]
    public List<ScatterLayer> layers = new List<ScatterLayer>();

    [Header("Ground detection")]
    [Tooltip("Rays are fired down from this height above the generator to find the ground surface.")]
    public float rayStartHeight = 50f;

    [Header("Preset setup")]
    [Tooltip("Where 'Setup Forest Preset' looks for your PP_ prefabs. If the folder doesn't exist it searches the whole project.")]
    public string prefabFolder = "Assets/Prefabs/Forest Level";

    const string GroundRootName = "Generated_Ground";
    const string ScatterRootName = "Generated_Scatter";
    const string FloorRootName = "Generated_Floor";

#if UNITY_EDITOR
    // ------------------------------------------------------------------
    // MAIN ENTRY POINT
    // ------------------------------------------------------------------

    [ContextMenu("Generate Forest")]
    public void Generate()
    {
        if (!Validate()) return;

        Undo.SetCurrentGroupName("Generate Forest");
        int undoGroup = Undo.GetCurrentGroup();

        Clear();

        var rng = new System.Random(seed);

        // 'size' is the SPACING between tile centres (real tile size shrunk by the overlap fraction).
        float size = ResolveTileSize() * (1f - tileOverlap);

        Vector3 corner = transform.position - new Vector3(gridWidth * size * 0.5f, 0f, gridHeight * size * 0.5f);

        Transform groundRoot = CreateRoot(GroundRootName, transform);
        Transform scatterRoot = CreateRoot(ScatterRootName, transform);

        CellType[,] types = BuildCellTypes(rng, out Dictionary<Vector2Int, float> pathYaw);

        SpawnGround(rng, types, pathYaw, corner, size, groundRoot);

        // Newly created colliders aren't known to physics until it syncs. Without this the rays below miss.
        Physics.SyncTransforms();

        if (addWalkableFloor) BuildWalkableFloor(types, corner, size, groundRoot);

        ScatterAll(rng, types, corner, size, groundRoot, scatterRoot);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[ForestGenerator] Done. Seed {seed}, grid {gridWidth}x{gridHeight}, tile spacing {size:0.##}.");
    }

    public void Clear()
    {
        DestroyChildNamed(GroundRootName);
        DestroyChildNamed(ScatterRootName);
        DestroyChildNamed(FloorRootName);
    }

    // ------------------------------------------------------------------
    // ONE-CLICK PRESET: finds prefabs by name and fills the layers with sensible values
    // ------------------------------------------------------------------

    [ContextMenu("Setup Forest Preset")]
    public void SetupPreset()
    {
        Undo.RecordObject(this, "Setup Forest Preset");

        meadowPrefabs = FindPrefabs("PP_Meadow", "Path");
        lakePrefabs = FindPrefabs("PP_Lake_Ground");
        pathPrefabs = FindPrefabs("PP_Meadow_Path");

        if (meadowPrefabs.Length == 0)
        {
            Debug.LogError($"[ForestGenerator] No prefab starting with 'PP_Meadow' found. Check the 'Prefab Folder' field ({prefabFolder}).");
            return;
        }

        // Everything below is expressed relative to the tile size, so the preset works whatever scale your tiles are.
        float t = ResolveTileSize();
        float area = (gridWidth * gridHeight) / 64f;        // preset counts are tuned for an 8x8 grid

        layers = new List<ScatterLayer>
        {
            //          name        prefabs                     attempts            radius   noiseScale  thresh  minS  maxS  yOff   tilt  align
            MakeLayer("Rocks",      FindPrefabs("PP_Rock"),     Mathf.RoundToInt(150  * area), 0.15f * t, 1.0f / t, 0.35f, 0.7f, 1.4f, -0.1f, 10f, true),
            MakeLayer("Pine Trees", FindPrefabs("PP_Tree"),     Mathf.RoundToInt(700  * area), 0.20f * t, 0.4f / t, 0.45f, 0.8f, 1.3f,  0f,    0f,  false),
            MakeLayer("Birch Trees",FindPrefabs("PP_Birch_Tree"),Mathf.RoundToInt(450 * area), 0.18f * t, 0.6f / t, 0.50f, 0.8f, 1.2f,  0f,    0f,  false),
            MakeLayer("Mushrooms",  FindPrefabs("PP_Mushroom"), Mathf.RoundToInt(200  * area), 0.05f * t, 1.2f / t, 0.55f, 0.8f, 1.2f,  0f,    8f,  true),
            MakeLayer("Flowers",    FindPrefabs("PP_Sunflower"),Mathf.RoundToInt(250  * area), 0.07f * t, 0.8f / t, 0.50f, 0.8f, 1.3f,  0f,    5f,  true),
            MakeLayer("Grass",      FindPrefabs("PP_Grass"),    Mathf.RoundToInt(2500 * area), 0f,        1.5f / t, 0.00f, 0.8f, 1.3f,  0f,    5f,  true),
        };

        EditorUtility.SetDirty(this);
        Debug.Log($"[ForestGenerator] Preset applied (tile size {t:0.##}). Meadow {meadowPrefabs.Length}, Lake {lakePrefabs.Length}, Path {pathPrefabs.Length}. " +
                  "Check the Console/Inspector for any layer with an empty Prefabs list, then click Generate.");
        foreach (var l in layers)
            if (l.prefabs == null || l.prefabs.Length == 0)
                Debug.LogWarning($"[ForestGenerator] Layer '{l.name}' found no prefabs. Drag them in manually.");
    }

    static ScatterLayer MakeLayer(string name, GameObject[] prefabs, int attempts, float radius, float noiseScale,
                                  float noiseThreshold, float minScale, float maxScale, float yOffset, float tilt, bool align)
    {
        return new ScatterLayer
        {
            name = name,
            prefabs = prefabs,
            attempts = attempts,
            radius = radius,
            noiseScale = noiseScale,
            noiseThreshold = noiseThreshold,
            scaleRange = new Vector2(minScale, maxScale),
            yOffset = yOffset,
            randomTilt = tilt,
            alignToGround = align,
            onMeadow = true,
            onLake = false,
            onPath = false
        };
    }

    // Finds prefab assets whose NAME starts with 'startsWith' and contains none of 'mustNotContain'.
    GameObject[] FindPrefabs(string startsWith, params string[] mustNotContain)
    {
        string filter = "t:Prefab " + startsWith;
        string[] guids = AssetDatabase.IsValidFolder(prefabFolder)
            ? AssetDatabase.FindAssets(filter, new[] { prefabFolder })
            : AssetDatabase.FindAssets(filter);

        var list = new List<GameObject>();
        foreach (string guid in guids)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go == null || !go.name.StartsWith(startsWith)) continue;

            bool excluded = false;
            foreach (string bad in mustNotContain)
                if (go.name.Contains(bad)) excluded = true;
            if (!excluded) list.Add(go);
        }
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list.ToArray();
    }

    // ------------------------------------------------------------------
    // STEP 1: CELL TYPES
    // ------------------------------------------------------------------

    CellType[,] BuildCellTypes(System.Random rng, out Dictionary<Vector2Int, float> pathYaw)
    {
        var types = new CellType[gridWidth, gridHeight];

        float offX = Range(rng, 0f, 1000f);
        float offZ = Range(rng, 0f, 1000f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                float n = Mathf.PerlinNoise(x * lakeNoiseScale + offX, z * lakeNoiseScale + offZ);
                types[x, z] = n < lakeThreshold ? CellType.Lake : CellType.Meadow;
            }
        }

        pathYaw = new Dictionary<Vector2Int, float>();
        for (int i = 0; i < pathCount; i++)
            CarvePath(rng, i % 2 == 0, pathYaw);

        foreach (var kv in pathYaw) types[kv.Key.x, kv.Key.y] = CellType.Path;   // Vector2Int.y is our Z

        return types;
    }

    void CarvePath(System.Random rng, bool alongX, Dictionary<Vector2Int, float> pathYaw)
    {
        int mainLen = alongX ? gridWidth : gridHeight;
        int sideLen = alongX ? gridHeight : gridWidth;
        int side = rng.Next(sideLen);

        for (int main = 0; main < mainLen; main++)
        {
            Vector2Int cell = alongX ? new Vector2Int(main, side) : new Vector2Int(side, main);
            pathYaw[cell] = YawFor(alongX);

            if (rng.NextDouble() < pathWander)
            {
                int step = rng.Next(2) == 0 ? -1 : 1;
                int newSide = Mathf.Clamp(side + step, 0, sideLen - 1);
                if (newSide != side)
                {
                    side = newSide;
                    Vector2Int jog = alongX ? new Vector2Int(main, side) : new Vector2Int(side, main);
                    pathYaw[jog] = YawFor(!alongX);
                }
            }
        }
    }

    float YawFor(bool alongX)
    {
        return (alongX == pathRunsAlongX) ? 0f : 90f;
    }

    // ------------------------------------------------------------------
    // STEP 2: GROUND TILES
    // ------------------------------------------------------------------

    void SpawnGround(System.Random rng, CellType[,] types, Dictionary<Vector2Int, float> pathYaw,
                     Vector3 corner, float size, Transform groundRoot)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GameObject[] pool;
                float yaw;

                if (types[x, z] == CellType.Path)
                {
                    pool = pathPrefabs;
                    yaw = pathYaw[new Vector2Int(x, z)];
                }
                else
                {
                    pool = types[x, z] == CellType.Lake ? lakePrefabs : meadowPrefabs;
                    yaw = randomRotateGround ? 90f * rng.Next(4) : 0f;
                }

                if (pool == null || pool.Length == 0) continue;

                GameObject prefab = pool[rng.Next(pool.Length)];
                Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

                // Where the tile's visual centre should end up.
                Vector3 centre = corner + new Vector3((x + 0.5f) * size, 0f, (z + 0.5f) * size);

                // If the prefab's pivot isn't at its visual centre, shift it back. The offset spins with the tile,
                // so we rotate it by the same yaw before subtracting.
                Vector3 pos = compensatePivot ? centre - yawRot * PivotOffset(prefab) : centre;

                Spawn(prefab, pos, yawRot, groundRoot);
            }
        }
    }

    // Horizontal distance from the prefab's pivot to the centre of its renderers (height ignored).
    static Vector3 PivotOffset(GameObject prefab)
    {
        var renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Vector3.zero;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        Vector3 offset = b.center - prefab.transform.position;
        offset.y = 0f;
        return offset;
    }

    // ------------------------------------------------------------------
    // STEP 2b: WALKABLE FLOOR (invisible safety net under non-lake cells)
    // ------------------------------------------------------------------

    void BuildWalkableFloor(CellType[,] types, Vector3 corner, float size, Transform groundRoot)
    {
        // Find the lowest ground surface among non-lake cells, so the floor never pokes above any tile.
        float lowest = float.MaxValue;
        bool any = false;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (types[x, z] == CellType.Lake) continue;
                Vector3 c = corner + new Vector3((x + 0.5f) * size, 0f, (z + 0.5f) * size);
                if (TryGetGround(c, groundRoot, out Vector3 p, out _))
                {
                    lowest = Mathf.Min(lowest, p.y);
                    any = true;
                }
            }
        }
        if (!any) return;

        float top = lowest - floorDrop;
        Vector3 gridCentre = corner + new Vector3(gridWidth * size * 0.5f, 0f, gridHeight * size * 0.5f);

        var floor = new GameObject(FloorRootName);
        floor.transform.SetParent(transform, false);
        floor.transform.position = new Vector3(gridCentre.x, top, gridCentre.z);
        Undo.RegisterCreatedObjectUndo(floor, "Generate Forest");

        // One box per non-lake cell (10% oversize so neighbours overlap). Lake cells stay open so lakes remain lakes.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (types[x, z] == CellType.Lake) continue;
                Vector3 c = corner + new Vector3((x + 0.5f) * size, 0f, (z + 0.5f) * size);

                var box = floor.AddComponent<BoxCollider>();
                box.size = new Vector3(size * 1.1f, floorThickness, size * 1.1f);
                box.center = new Vector3(c.x - gridCentre.x, -floorThickness * 0.5f, c.z - gridCentre.z);
            }
        }
    }

    // ------------------------------------------------------------------
    // STEP 3: DECORATION SCATTER
    // ------------------------------------------------------------------

    void ScatterAll(System.Random rng, CellType[,] types, Vector3 corner, float size,
                    Transform groundRoot, Transform scatterRoot)
    {
        var placed = new List<(Vector3 pos, float radius)>();

        float areaX = gridWidth * size;
        float areaZ = gridHeight * size;

        foreach (var layer in layers)
        {
            if (layer.prefabs == null || layer.prefabs.Length == 0) continue;

            Transform layerRoot = CreateRoot(layer.name, scatterRoot);

            float offX = Range(rng, 0f, 1000f);
            float offZ = Range(rng, 0f, 1000f);

            int attempts = Mathf.RoundToInt(layer.attempts * densityMultiplier);
            int count = 0;
            for (int i = 0; i < attempts; i++)
            {
                float lx = Range(rng, 0f, areaX);
                float lz = Range(rng, 0f, areaZ);

                int cx = Mathf.Clamp((int)(lx / size), 0, gridWidth - 1);
                int cz = Mathf.Clamp((int)(lz / size), 0, gridHeight - 1);
                if (!AllowedOn(layer, types[cx, cz])) continue;

                float n = Mathf.PerlinNoise(lx * layer.noiseScale + offX, lz * layer.noiseScale + offZ);
                if (n < layer.noiseThreshold) continue;

                Vector3 flat = corner + new Vector3(lx, 0f, lz);
                if (!TryGetGround(flat, groundRoot, out Vector3 point, out Vector3 normal)) continue;

                bool usesSpacing = layer.radius > 0f;
                if (usesSpacing && Overlaps(placed, point, layer.radius)) continue;

                float yaw = Range(rng, 0f, 360f);
                float tiltX = Range(rng, -layer.randomTilt, layer.randomTilt);
                float tiltZ = Range(rng, -layer.randomTilt, layer.randomTilt);
                Quaternion rot = Quaternion.Euler(tiltX, yaw, tiltZ);
                if (layer.alignToGround) rot = Quaternion.FromToRotation(Vector3.up, normal) * rot;

                GameObject go = Spawn(layer.prefabs[rng.Next(layer.prefabs.Length)],
                                      point + Vector3.up * layer.yOffset, rot, layerRoot);
                if (go == null) continue;

                go.transform.localScale *= Range(rng, layer.scaleRange.x, layer.scaleRange.y);

                if (usesSpacing) placed.Add((point, layer.radius));
                count++;
            }

            Debug.Log($"[ForestGenerator] {layer.name}: placed {count} / {attempts} attempts.");
        }
    }

    static bool AllowedOn(ScatterLayer layer, CellType type)
    {
        switch (type)
        {
            case CellType.Meadow: return layer.onMeadow;
            case CellType.Lake:   return layer.onLake;
            default:              return layer.onPath;
        }
    }

    static bool Overlaps(List<(Vector3 pos, float radius)> placed, Vector3 point, float radius)
    {
        foreach (var p in placed)
        {
            float dx = p.pos.x - point.x;
            float dz = p.pos.z - point.z;
            float min = p.radius + radius;
            if (dx * dx + dz * dz < min * min) return true;
        }
        return false;
    }

    // Only counts hits on GROUND tiles (children of groundRoot), so the floor and scattered objects are ignored.
    bool TryGetGround(Vector3 flat, Transform groundRoot, out Vector3 point, out Vector3 normal)
    {
        Vector3 origin = new Vector3(flat.x, transform.position.y + rayStartHeight, flat.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, rayStartHeight * 2f);

        point = default;
        normal = Vector3.up;
        float best = float.MaxValue;
        bool found = false;

        foreach (var hit in hits)
        {
            if (!hit.transform.IsChildOf(groundRoot)) continue;
            if (hit.distance < best)
            {
                best = hit.distance;
                point = hit.point;
                normal = hit.normal;
                found = true;
            }
        }
        return found;
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (go == null) return null;
        // Our rotation applied ON TOP of the prefab's own (FBX objects carry a baked-in -90 on X).
        go.transform.SetPositionAndRotation(pos, rot * prefab.transform.rotation);
        Undo.RegisterCreatedObjectUndo(go, "Generate Forest");
        return go;
    }

    Transform CreateRoot(string rootName, Transform parent)
    {
        var go = new GameObject(rootName);
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Generate Forest");
        return go.transform;
    }

    void DestroyChildNamed(string childName)
    {
        Transform child;
        while ((child = transform.Find(childName)) != null)
            Undo.DestroyObjectImmediate(child.gameObject);
    }

    // The REAL tile size (before overlap). Uses tileSize if set, otherwise measures the first Meadow prefab.
    float ResolveTileSize()
    {
        if (tileSize > 0f) return tileSize;
        if (meadowPrefabs == null || meadowPrefabs.Length == 0 || meadowPrefabs[0] == null) return 10f;

        var renderers = meadowPrefabs[0].GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 10f;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float measured = Mathf.Max(b.size.x, b.size.z);
        Debug.Log($"[ForestGenerator] Auto-measured tile size: {measured:0.##}. If tiles overlap or have gaps, type the real number into Tile Size.");
        return measured;
    }

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    bool Validate()
    {
        if (meadowPrefabs == null || meadowPrefabs.Length == 0 || meadowPrefabs[0] == null)
        {
            Debug.LogError("[ForestGenerator] Assign at least one Meadow prefab (or click 'Setup Forest Preset').");
            return false;
        }
        return true;
    }
#endif
}