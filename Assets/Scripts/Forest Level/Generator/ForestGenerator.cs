using System.Collections.Generic;   // List<> and Dictionary<>
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;                  // PrefabUtility + Undo. Editor-only, so it's wrapped in #if
#endif

/// <summary>
/// Builds a forest level out of tile prefabs, then scatters trees/rocks/mushrooms on top.
/// Runs in EDIT MODE (button in the Inspector) so the result is a normal, saved scene hierarchy.
///
/// Pipeline:  1) decide what each grid cell is (Meadow / Lake / Path)
///            2) spawn the ground tile for each cell
///            3) scatter decoration layers on the ground using noise + spacing rules
/// </summary>
public class ForestGenerator : MonoBehaviour
{

    // TODO: Addd Spline to generate the path maybe? the grounds do not overlap meaning its not walkable for the player.

    
    // ------------------------------------------------------------------
    // TYPES
    // ------------------------------------------------------------------

    // What a single grid cell is. Scatter layers use this to decide where they're allowed to grow.
    enum CellType { Meadow, Lake, Path }

    // One "layer" of decoration (e.g. Pine Trees, Birch Trees, Rocks, Mushrooms).
    // [Serializable] makes Unity show it in the Inspector as an expandable list element.
    [System.Serializable]
    public class ScatterLayer
    {
        public string name = "Layer";                       // also used as the name of the folder object in the Hierarchy
        public GameObject[] prefabs;                        // one is picked at random per placement (e.g. PP_Tree variants)

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

        // Which ground types this layer may spawn on.
        public bool onMeadow = true;
        public bool onLake = false;
        public bool onPath = false;
    }

    // ------------------------------------------------------------------
    // SETTINGS (everything you can tweak in the Inspector)
    // ------------------------------------------------------------------

    [Header("Seed")]
    [Tooltip("Same seed + same settings = identical forest. Change it for a different forest.")]
    public int seed = 1234;

    [Header("Grid")]
    [Min(1)] public int gridWidth = 8;                      // number of tiles along X
    [Min(1)] public int gridHeight = 8;                     // number of tiles along Z
    [Tooltip("World size of ONE tile. Leave at 0 to auto-measure from the first Meadow prefab (it logs what it found).")]
    public float tileSize = 0f;

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
    [Tooltip("Chance per step that the path sidesteps one tile. 0 = dead straight, 0.5 = very wiggly.")]
    [Range(0f, 1f)] public float pathWander = 0.25f;
    [Tooltip("Tick if the path on your path tile runs along world X when the tile has rotation 0 (it does in your screenshot).")]
    public bool pathRunsAlongX = true;

    [Header("Decoration layers (order matters: earlier layers claim space first)")]
    public List<ScatterLayer> layers = new List<ScatterLayer>();

    [Header("Ground detection")]
    [Tooltip("Rays are fired down from this height above the generator to find the ground surface.")]
    public float rayStartHeight = 50f;

    // Names of the two folder objects the generator creates. Clear() deletes only these, so anything
    // you hand-place elsewhere under this object survives regeneration.
    const string GroundRootName = "Generated_Ground";
    const string ScatterRootName = "Generated_Scatter";

#if UNITY_EDITOR
    // ------------------------------------------------------------------
    // MAIN ENTRY POINT
    // ------------------------------------------------------------------

    [ContextMenu("Generate Forest")]    // also lets you right-click the component header to run it
    public void Generate()
    {
        if (!Validate()) return;        // bail out early with a clear error if setup is incomplete

        // Group every create/destroy below into ONE undo step, so Ctrl+Z reverts the whole generation.
        Undo.SetCurrentGroupName("Generate Forest");
        int undoGroup = Undo.GetCurrentGroup();

        Clear();                        // remove the previous result

        // System.Random with our seed = reproducible. We use this instead of UnityEngine.Random so we
        // don't disturb Unity's global random state.
        var rng = new System.Random(seed);

        float size = ResolveTileSize();

        // The grid is centred on this object. 'corner' is the world position of the grid's (0,0) corner.
        Vector3 corner = transform.position - new Vector3(gridWidth * size * 0.5f, 0f, gridHeight * size * 0.5f);

        Transform groundRoot = CreateRoot(GroundRootName, transform);
        Transform scatterRoot = CreateRoot(ScatterRootName, transform);

        // STEP 1: decide what every cell is.
        CellType[,] types = BuildCellTypes(rng, out Dictionary<Vector2Int, float> pathYaw);

        // STEP 2: spawn the ground tiles.
        SpawnGround(rng, types, pathYaw, corner, size, groundRoot);

        // Newly created colliders aren't known to the physics system until it syncs. Without this,
        // the raycasts in step 3 would pass straight through the fresh tiles.
        Physics.SyncTransforms();

        // STEP 3: scatter decoration.
        ScatterAll(rng, types, corner, size, groundRoot, scatterRoot);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[ForestGenerator] Done. Seed {seed}, grid {gridWidth}x{gridHeight}, tile size {size:0.##}.");
    }

    public void Clear()
    {
        DestroyChildNamed(GroundRootName);
        DestroyChildNamed(ScatterRootName);
    }

    // ------------------------------------------------------------------
    // STEP 1: CELL TYPES (lakes from noise, paths from a wandering walk)
    // ------------------------------------------------------------------

    CellType[,] BuildCellTypes(System.Random rng, out Dictionary<Vector2Int, float> pathYaw)
    {
        var types = new CellType[gridWidth, gridHeight];    // [x, z] grid, defaults to Meadow (enum value 0)

        // Random offset so different seeds sample a different part of the (infinite) noise field.
        float offX = Range(rng, 0f, 1000f);
        float offZ = Range(rng, 0f, 1000f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // PerlinNoise gives smooth values roughly 0..1. Neighbouring cells get similar values,
                // which is what makes lakes form blobs instead of random speckles.
                float n = Mathf.PerlinNoise(x * lakeNoiseScale + offX, z * lakeNoiseScale + offZ);
                types[x, z] = n < lakeThreshold ? CellType.Lake : CellType.Meadow;
            }
        }

        // pathYaw maps cell -> rotation (degrees) that the path tile needs at that cell.
        pathYaw = new Dictionary<Vector2Int, float>();
        for (int i = 0; i < pathCount; i++)
        {
            // Even paths run West->East (travel along grid X), odd ones North->South (along grid Z).
            CarvePath(rng, i % 2 == 0, pathYaw);
        }

        // Paths win over lakes: overwrite those cells' type.
        foreach (var kv in pathYaw) types[kv.Key.x, kv.Key.y] = CellType.Path;   // Vector2Int.y is our Z

        return types;
    }

    // Walks across the grid edge-to-edge, occasionally sidestepping, and records every cell it touches.
    void CarvePath(System.Random rng, bool alongX, Dictionary<Vector2Int, float> pathYaw)
    {
        int mainLen = alongX ? gridWidth : gridHeight;      // how far we travel
        int sideLen = alongX ? gridHeight : gridWidth;      // how much room we have to wander sideways
        int side = rng.Next(sideLen);                       // random starting row (or column) on the first edge

        for (int main = 0; main < mainLen; main++)
        {
            // Mark the current cell. Convert (main, side) back to grid (x, z) depending on direction.
            Vector2Int cell = alongX ? new Vector2Int(main, side) : new Vector2Int(side, main);
            pathYaw[cell] = YawFor(alongX);                 // straight tile rotated to match travel direction

            // Maybe sidestep one tile. rng.NextDouble() is 0..1, so "< pathWander" is a pathWander% chance.
            if (rng.NextDouble() < pathWander)
            {
                int step = rng.Next(2) == 0 ? -1 : 1;                       // left or right
                int newSide = Mathf.Clamp(side + step, 0, sideLen - 1);     // stay inside the grid
                if (newSide != side)
                {
                    side = newSide;
                    // The sidestep cell is travelled in the OTHER direction, so it gets the other rotation.
                    Vector2Int jog = alongX ? new Vector2Int(main, side) : new Vector2Int(side, main);
                    pathYaw[jog] = YawFor(!alongX);
                }
            }
        }
    }

    // Rotation for a straight path tile travelling along X (alongX = true) or Z (false).
    float YawFor(bool alongX)
    {
        // If the tile's path already runs along X at yaw 0, travelling along X needs 0 and along Z needs 90.
        // If the tile's path runs along Z at yaw 0, it's the other way round.
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
                    yaw = pathYaw[new Vector2Int(x, z)];                    // exact rotation worked out in CarvePath
                }
                else
                {
                    pool = types[x, z] == CellType.Lake ? lakePrefabs : meadowPrefabs;
                    yaw = randomRotateGround ? 90f * rng.Next(4) : 0f;      // 0, 90, 180 or 270
                }

                if (pool == null || pool.Length == 0) continue;             // nothing assigned for this type: leave a hole

                // Tile centre: corner + half a tile (to reach the middle) + whole tiles along each axis.
                Vector3 pos = corner + new Vector3((x + 0.5f) * size, 0f, (z + 0.5f) * size);
                Spawn(pool[rng.Next(pool.Length)], pos, Quaternion.Euler(0f, yaw, 0f), groundRoot);
            }
        }
    }

    // ------------------------------------------------------------------
    // STEP 3: DECORATION SCATTER
    // ------------------------------------------------------------------

    void ScatterAll(System.Random rng, CellType[,] types, Vector3 corner, float size,
                    Transform groundRoot, Transform scatterRoot)
    {
        // Every object placed so far (across ALL layers), so later layers avoid earlier ones.
        var placed = new List<(Vector3 pos, float radius)>();

        float areaX = gridWidth * size;
        float areaZ = gridHeight * size;

        foreach (var layer in layers)
        {
            if (layer.prefabs == null || layer.prefabs.Length == 0) continue;

            Transform layerRoot = CreateRoot(layer.name, scatterRoot);      // keeps the Hierarchy tidy

            // Each layer samples its own noise region, so trees and rocks don't clump in the same places.
            float offX = Range(rng, 0f, 1000f);
            float offZ = Range(rng, 0f, 1000f);

            int count = 0;
            for (int i = 0; i < layer.attempts; i++)
            {
                // Random point inside the grid, measured from the corner (so always >= 0, which Perlin likes).
                float lx = Range(rng, 0f, areaX);
                float lz = Range(rng, 0f, areaZ);

                // Which cell is this point in? Clamp guards against lx == areaX landing one past the end.
                int cx = Mathf.Clamp((int)(lx / size), 0, gridWidth - 1);
                int cz = Mathf.Clamp((int)(lz / size), 0, gridHeight - 1);
                if (!AllowedOn(layer, types[cx, cz])) continue;             // wrong ground type for this layer

                // Noise gate: only keep spots where the noise is high enough -> clumps and clearings.
                float n = Mathf.PerlinNoise(lx * layer.noiseScale + offX, lz * layer.noiseScale + offZ);
                if (n < layer.noiseThreshold) continue;

                // Find the real ground height (tiles have bumps) and its slope.
                Vector3 flat = corner + new Vector3(lx, 0f, lz);
                if (!TryGetGround(flat, groundRoot, out Vector3 point, out Vector3 normal)) continue;

                // Radius 0 = "free" layer (e.g. grass): no spacing rule, and it never blocks other layers.
                // Skipping the check also avoids the slow brute-force loop for thousands of grass tufts.
                bool usesSpacing = layer.radius > 0f;
                if (usesSpacing && Overlaps(placed, point, layer.radius)) continue;   // too close to something already there

                // Build the rotation: random spin around Y, plus optional random lean...
                float yaw = Range(rng, 0f, 360f);
                float tiltX = Range(rng, -layer.randomTilt, layer.randomTilt);
                float tiltZ = Range(rng, -layer.randomTilt, layer.randomTilt);
                Quaternion rot = Quaternion.Euler(tiltX, yaw, tiltZ);
                // ...and optionally tilt the whole thing to sit flush on the slope.
                if (layer.alignToGround) rot = Quaternion.FromToRotation(Vector3.up, normal) * rot;

                GameObject go = Spawn(layer.prefabs[rng.Next(layer.prefabs.Length)],
                                      point + Vector3.up * layer.yOffset, rot, layerRoot);
                if (go == null) continue;

                go.transform.localScale *= Range(rng, layer.scaleRange.x, layer.scaleRange.y);

                if (usesSpacing) placed.Add((point, layer.radius));   // free layers (grass) don't claim space
                count++;
            }

            // If this says 0 for every layer, the ground has no collider (see Validate / raycast notes).
            Debug.Log($"[ForestGenerator] {layer.name}: placed {count} / {layer.attempts} attempts.");
        }
    }

    static bool AllowedOn(ScatterLayer layer, CellType type)
    {
        switch (type)
        {
            case CellType.Meadow: return layer.onMeadow;
            case CellType.Lake:   return layer.onLake;
            default:              return layer.onPath;   // CellType.Path
        }
    }

    // Brute-force spacing check. Fine for a few thousand objects at edit time.
    // (If you ever scatter 20k+, swap this for a spatial hash grid.)
    static bool Overlaps(List<(Vector3 pos, float radius)> placed, Vector3 point, float radius)
    {
        foreach (var p in placed)
        {
            float dx = p.pos.x - point.x;
            float dz = p.pos.z - point.z;                   // ignore height: spacing is a top-down thing
            float min = p.radius + radius;
            if (dx * dx + dz * dz < min * min) return true; // compare squared distances: avoids a slow sqrt
        }
        return false;
    }

    // Fires a ray straight down and returns the highest surface that belongs to a GROUND tile.
    // We use RaycastAll and filter, so a tree or rock already placed can't be mistaken for ground.
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
            if (!hit.transform.IsChildOf(groundRoot)) continue;     // not a ground tile: ignore
            if (hit.distance < best)                                // keep the closest (= top-most) ground hit
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

    // Spawns a prefab as a real prefab INSTANCE (keeps the blue prefab link, unlike Instantiate()).
    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (go == null) return null;                                // happens if 'prefab' is a scene object, not an asset
        // 'rot * prefab rotation' = our rotation applied ON TOP of the prefab's own one.
        // FBX objects carry a baked-in -90 degrees on X; replacing it would lay everything on its side.
        go.transform.SetPositionAndRotation(pos, rot * prefab.transform.rotation);
        Undo.RegisterCreatedObjectUndo(go, "Generate Forest");      // so Ctrl+Z can remove it
        return go;
    }

    Transform CreateRoot(string rootName, Transform parent)
    {
        var go = new GameObject(rootName);
        go.transform.SetParent(parent, false);                      // false = keep local position (0,0,0) under the parent
        Undo.RegisterCreatedObjectUndo(go, "Generate Forest");
        return go.transform;
    }

    void DestroyChildNamed(string childName)
    {
        Transform child;
        while ((child = transform.Find(childName)) != null)         // loop in case an old run left duplicates
            Undo.DestroyObjectImmediate(child.gameObject);          // undoable delete, works in edit mode
    }

    // Uses tileSize if you set one; otherwise measures the first Meadow prefab's renderers.
    float ResolveTileSize()
    {
        if (tileSize > 0f) return tileSize;

        var renderers = meadowPrefabs[0].GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 10f;                      // nothing to measure: arbitrary fallback

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);       // grow the box to contain every renderer
        float measured = Mathf.Max(b.size.x, b.size.z);
        Debug.Log($"[ForestGenerator] Auto-measured tile size: {measured:0.##}. If tiles overlap or have gaps, type the real number into Tile Size.");
        return measured;
    }

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);         // NextDouble is 0..1; stretch it to min..max
    }

    bool Validate()
    {
        if (meadowPrefabs == null || meadowPrefabs.Length == 0 || meadowPrefabs[0] == null)
        {
            Debug.LogError("[ForestGenerator] Assign at least one Meadow prefab (it's also used to measure tile size).");
            return false;
        }
        return true;
    }
#endif
}