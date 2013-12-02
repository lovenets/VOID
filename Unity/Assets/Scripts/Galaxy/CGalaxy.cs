﻿using UnityEngine;
using System.Collections;

public class CGalaxy : CNetworkMonoBehaviour
{
    ///////////////////////////////////////////////////////////////////////////
    // Objects:

    public struct SGridCellPos
    {
        public int x;
        public int y;
        public int z;

        public SGridCellPos(int _x, int _y, int _z) { x = _x; y = _y; z = _z; }

        public static SGridCellPos operator +(SGridCellPos lhs, SGridCellPos rhs) { return new SGridCellPos(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z); }
        public static SGridCellPos operator -(SGridCellPos lhs, SGridCellPos rhs) { return new SGridCellPos(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z); }
        //public static bool operator ==(SGridCellPos lhs, SGridCellPos rhs) { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z; }
        //public static bool operator !=(SGridCellPos lhs, SGridCellPos rhs) { return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z; }
    }

    class CGridCellContent
    {
        public CGridCellContent(bool alternatorInitialValue, bool loaded = false) { mAlternator = alternatorInitialValue; mLoaded = loaded; }
        public bool mAlternator;   // This is used for culling purposes.
        public bool mLoaded;    // Cell loading is drawn out over time. This shows whether the cell is ready or waiting.
    }

    class CRegisteredObserver
    {
        public GameObject mEntity;
        public float mBoundingRadius;   // Bounding sphere.

        public CRegisteredObserver(GameObject entity, float boundingRadius) { mEntity = entity; mBoundingRadius = boundingRadius; }
    }

    class CRegisteredGubbin
    {
        public GameObject mEntity;
        public float mBoundingRadius;   // Bounding sphere.
        public ushort mNetworkViewID;
        public bool mAlternator;   // This is used for culling purposes.

        public CRegisteredGubbin(GameObject entity, float boundingRadius, ushort networkViewID, bool alternatorValue) { mEntity = entity; mBoundingRadius = boundingRadius; mNetworkViewID = networkViewID; mAlternator = alternatorValue; }
    }

    public enum ENoiseLayer : uint
    {
        AsteroidDensity,
        FogDensity,
        MAX
    }

    enum ESkybox : uint
    {
        Composite,
        Solid,
        MAX
    }

    ///////////////////////////////////////////////////////////////////////////
    // Variables:

    private static CGalaxy sGalaxy = null;
    public static CGalaxy instance { get { return sGalaxy; } }

    private PerlinSimplexNoise[] mNoises = new PerlinSimplexNoise[(uint)ENoiseLayer.MAX];
    protected CNetworkVar<int>[] mNoiseSeeds = new CNetworkVar<int>[(uint)ENoiseLayer.MAX];

    private Cubemap[] mSkyboxes = new Cubemap[(uint)ESkybox.MAX];

    private SGridCellPos mCentreCell = new SGridCellPos(0, 0, 0);    // All cells are offset by this cell.
    protected CNetworkVar<int> mCentreCellX;
    protected CNetworkVar<int> mCentreCellY;
    protected CNetworkVar<int> mCentreCellZ;

    private System.Collections.Generic.List<Transform> mShiftableTransforms = new System.Collections.Generic.List<Transform>();    // When everything moves too far in any direction, the transforms of these registered GameObjects are shifted back.
    private System.Collections.Generic.List<CRegisteredObserver> mObservers = new System.Collections.Generic.List<CRegisteredObserver>(); // Cells in the grid are loaded and unloaded based on proximity to observers.
    private System.Collections.Generic.List<GalaxyIE> mGalaxyIEs;   // Is only instantiated when this galaxy is ready to update galaxyIEs.
    private System.Collections.Generic.List<CRegisteredGubbin> mGubbins;    // Gubbins ("space things") are unloaded based on proximity to cells.
    private System.Collections.Generic.Dictionary<SGridCellPos, CGridCellContent> mGrid;
    private System.Collections.Generic.Queue<SGridCellPos> mCellsToLoad;

    private float mfGalaxySize = 1391000000.0f; // (1.3 million kilometres) In metres cubed. Floats can increment up to 16777220.0f (16.7 million).
    protected CNetworkVar<float> mGalaxySize;
    public float galaxySize { get { return mfGalaxySize; } }

    private uint muiNumGridSubsets = 20; // Zero is just the one cell. Also, this is equivalent to the number of bits per axis required to acknowledge each cell (<= 2 for 1 byte, <= 5 for 2 bytes, <= 10 for 4 bytes, <= 21 for 8 bytes).
    protected CNetworkVar<uint> mNumGridSubsets;
    public uint numGridSubsets { get { return muiNumGridSubsets; } }

    private uint muiMaxAsteroidsPerCell = 5;
    protected CNetworkVar<uint> mMaxAsteroidsPerCell;
    public uint maxAsteroidsPerCell { get { return muiMaxAsteroidsPerCell; } }

    public const float mfTimeBetweenQueueCellsToLoad = 0.2f;
    private float mfTimeUntilNextQueueCellToLoad = 0.0f;
    public const float mfTimeBetweenCellUnloads = 0.1f;
    private float mfTimeUntilNextCellUnload = 0.0f;
    public const float mfTimeBetweenGubbinUnloads = 0.4f;
    private float mfTimeUntilNextGubbinUnload = 0.0f;
    public const float mfTimeBetweenShiftTests = 0.5f;
    private float mfTimeUntilNextShiftTest = 0.0f;
    public const float mfTimeBetweenCellLoads = 0.1f;
    private float mfTimeUntilNextCellLoad = 0.0f;

    private uint mNumExtraNeighbourCells = 3;   // Number of extra cells to load in every direction (i.e. load neighbours up to some distance).
    public uint numExtraNeighbourCells { get { return mNumExtraNeighbourCells; } }

    private bool mbValidCellValue = false;  // Used for culling cells that are too far away from observers.

    public float cellDiameter { get { return mfGalaxySize / numGridCellsInRow; } }
    public float cellRadius { get { return mfGalaxySize / (numGridCellsInRow*2u); } }
    public ulong numGridCells { get { /*return (uint)Mathf.Pow(8, muiGridSubsets);*/ ulong ul = 1; for (uint ui2 = 0; ui2 < muiNumGridSubsets; ++ui2)ul *= 8u; return ul; } }
    public uint numGridCellsInRow { get { /*return (uint)Mathf.Pow(2, muiGridSubsets);*/ uint ui = 1; for (uint ui2 = 0; ui2 < muiNumGridSubsets; ++ui2)ui *= 2; return ui; } }

    ///////////////////////////////////////////////////////////////////////////
    // Functions:

    public CGalaxy()
    {
        // Instantiate galaxy noises.
        for (uint ui = 0; ui < (uint)ENoiseLayer.MAX; ++ui)
            mNoises[ui] = new PerlinSimplexNoise();
    }

    void Start()
    {
        sGalaxy = this;

        // Fog and skybox are controlled by the galaxy.
        RenderSettings.fog = false;
        RenderSettings.skybox = null;

        //// Initialise galaxy noises.
        //for(uint ui = 0; ui < (uint)ENoiseLayer.MAX; ++ui)
        //    mNoises[ui] = new PerlinSimplexNoise();

        // Load skyboxes.
        string[] skyboxFaces = new string[6];
        skyboxFaces[0] = "Left";
        skyboxFaces[1] = "Right";
        skyboxFaces[2] = "Down";
        skyboxFaces[3] = "Up";
        skyboxFaces[4] = "Front";
        skyboxFaces[5] = "Back";

        Profiler.BeginSample("Initialise cubemap from 6 textures");
        for (uint uiSkybox = 0; uiSkybox < (uint)ESkybox.MAX; ++uiSkybox)    // For each skybox...
        {
            for (uint uiFace = 0; uiFace < 6; ++uiFace)  // For each face on the skybox...
            {
                Texture2D skyboxFace = Resources.Load("Textures/Galaxy/" + uiSkybox.ToString() + skyboxFaces[uiFace], typeof(Texture2D)) as Texture2D;  // Load the texture from file.
                if (!mSkyboxes[uiSkybox])
                    mSkyboxes[uiSkybox] = new Cubemap(skyboxFace.width, skyboxFace.format, false);
                mSkyboxes[uiSkybox].SetPixels(skyboxFace.GetPixels(), (CubemapFace)uiFace);
                Resources.UnloadAsset(skyboxFace);
            }

            mSkyboxes[uiSkybox].Apply(false, true);
        }
        Profiler.EndSample();

        //Profiler.BeginSample("Load cubemaps");
        //for (uint uiSkybox = 0; uiSkybox < (uint)ESkybox.MAX; ++uiSkybox)    // For each skybox...
        //    mSkyboxes[uiSkybox] = Resources.Load("Textures/Galaxy/" + uiSkybox.ToString() + "Cubemap", typeof(Cubemap)) as Cubemap;  // Load the cubemap texture from file.
        //Profiler.EndSample();

        // Galaxy is ready to update galaxyIEs.
        mGalaxyIEs = new System.Collections.Generic.List<GalaxyIE>();

        // Statistical data sometimes helps spot errors.
        Debug.Log("Galaxy is " + mfGalaxySize.ToString("n0") + " units³ with " + muiNumGridSubsets.ToString("n0") + " grid subsets, thus the " + numGridCells.ToString("n0") + " cells are " + (mfGalaxySize / numGridCellsInRow).ToString("n0") + " units in diameter and " + numGridCellsInRow.ToString("n0") + " cells in a row.");

        if (CNetwork.IsServer)
        {
            mGubbins = new System.Collections.Generic.List<CRegisteredGubbin>();
            mGrid = new System.Collections.Generic.Dictionary<SGridCellPos, CGridCellContent>();
            mCellsToLoad = new System.Collections.Generic.Queue<SGridCellPos>();

            // Seed galaxy noises through the network variable to sync the seed across all clients.
            for(uint ui = 0; ui < (uint)ENoiseLayer.MAX; ++ui)
                mNoiseSeeds[ui].Set(Random.Range(int.MinValue, int.MaxValue));
        }
    }

    void OnDestroy()
    {
        sGalaxy = null;
    }

    public override void InstanceNetworkVars()
    {
        Profiler.BeginSample("InstanceNetworkVars");

        for (uint ui = 0; ui < (uint)ENoiseLayer.MAX; ++ui)
            mNoiseSeeds[ui] = new CNetworkVar<int>(SyncNoiseSeed);

        mCentreCellX = new CNetworkVar<int>(SyncCentreCellX, mCentreCell.x);
        mCentreCellY = new CNetworkVar<int>(SyncCentreCellY, mCentreCell.y);
        mCentreCellZ = new CNetworkVar<int>(SyncCentreCellZ, mCentreCell.z);
        mGalaxySize = new CNetworkVar<float>(SyncGalaxySize, mfGalaxySize);
        mMaxAsteroidsPerCell = new CNetworkVar<uint>(SyncMaxAsteroidsPerCell, muiMaxAsteroidsPerCell);
        mNumGridSubsets = new CNetworkVar<uint>(SyncNumGridSubsets, muiNumGridSubsets);

        Profiler.EndSample();
    }

    void Update()
    {
        if (CNetwork.IsServer)
        {
            // Queue cells to load based on proximity to observers.
            mfTimeUntilNextQueueCellToLoad -= Time.deltaTime;
            if (mfTimeUntilNextQueueCellToLoad <= 0.0f)  // Running more than once per frame does nothing extra.
            {
                mfTimeUntilNextQueueCellToLoad = mfTimeBetweenQueueCellsToLoad;    // Drop the remainder as it is not required to run multiple times to make up for lag spikes.

                mbValidCellValue = !mbValidCellValue;   // Alternate the valid cell value. All cells within proximity of an observer will be updated, while all others will retain the old value making it easier to detect and cull them.

                // Load unloaded grid cells within proximity to observers.
                Profiler.BeginSample("Check for cells to queue for loading");
                foreach (CRegisteredObserver observer in mObservers)
                {
                    Vector3 observerPosition = observer.mEntity.transform.position;
                    SGridCellPos occupiedRelativeCell = PointToRelativeCell(observerPosition);
                    int iCellsInARow = 1 /*Centre cell*/ + (int)mNumExtraNeighbourCells * 2 /*Neighbouring cell rows*/ + (Mathf.CeilToInt((observer.mBoundingRadius / cellRadius) - 1) * 2);    // Centre point plus neighbours per axis.   E.g. 1,3,5,7,9...
                    int iNeighboursPerDirection = (iCellsInARow - 1) / 2;                                                                                                                       // Neighbours per direction.                E.g. 0,2,4,6,8...

                    for (int x = -iNeighboursPerDirection; x <= iNeighboursPerDirection; ++x)
                    {
                        for (int y = -iNeighboursPerDirection; y <= iNeighboursPerDirection; ++y)
                        {
                            for (int z = -iNeighboursPerDirection; z <= iNeighboursPerDirection; ++z)
                            {
                                // Check if this cell is loaded.
                                SGridCellPos neighbouringRelativeCell = new SGridCellPos(occupiedRelativeCell.x + x, occupiedRelativeCell.y + y, occupiedRelativeCell.z + z);
                                if (RelativeCellWithinProximityOfPoint(neighbouringRelativeCell, observerPosition, observer.mBoundingRadius + cellDiameter * mNumExtraNeighbourCells))
                                {
                                    SGridCellPos neighbouringAbsoluteCell = neighbouringRelativeCell + mCentreCell;
                                    CGridCellContent temp;
                                    if (mGrid.TryGetValue(neighbouringAbsoluteCell, out temp))   // Existing cell...
                                        temp.mAlternator = mbValidCellValue;    // Update alternator to indicate the cell is within proximity of an observer.
                                    else    // Not an existing cell...
                                        QueueCellToLoad(neighbouringAbsoluteCell);
                                }
                            }
                        }
                    }
                }
                Profiler.EndSample();
            }

            // Unload cells that are too far from any observers.
            mfTimeUntilNextCellUnload -= Time.deltaTime;
            if (mfTimeUntilNextCellUnload <= 0.0f)  // Running more than once per frame does nothing extra.
            {
                mfTimeUntilNextCellUnload = mfTimeBetweenCellUnloads;   // Drop the remainder as it is not required to run multiple times to make up for lag spikes.

                Profiler.BeginSample("Unload cells");
                bool restart;
                do
                {
                    restart = false;
                    foreach (System.Collections.Generic.KeyValuePair<SGridCellPos, CGridCellContent> absoluteCell in mGrid) // For every loaded cell...
                    {
                        if (absoluteCell.Value.mAlternator != mbValidCellValue)  // If the cell was not updated to the current alternator value...
                        {
                            // This cell is not within proximity of any observers.
                            UnloadAbsoluteCell(absoluteCell.Key); // Unload the cell.
                            restart = true;
                            break;
                        }
                    }
                } while (restart);
                Profiler.EndSample();
            }

            // Unload gubbins that are not within proximity to the cells.
            mfTimeUntilNextGubbinUnload -= Time.deltaTime;
            if (mfTimeUntilNextGubbinUnload <= 0.0f)  // Running more than once per frame does nothing extra.
            {
                mfTimeUntilNextGubbinUnload = mfTimeBetweenGubbinUnloads;   // Drop the remainder as it is not required to run multiple times to make up for lag spikes.

                // Find gubbins that are not within proximity to the cells.
                Profiler.BeginSample("Find gubbins");
                foreach (CRegisteredGubbin gubbin in mGubbins)
                {
                    Vector3 gubbinPosition = gubbin.mEntity.transform.position;
                    SGridCellPos occupiedRelativeCell = PointToRelativeCell(gubbinPosition);
                    int iCellsInARow = 1 + (Mathf.CeilToInt((gubbin.mBoundingRadius / cellRadius) - 1) * 2);    // Centre point plus neighbours per axis.   E.g. 1,3,5,7,9...
                    int iNeighboursPerDirection = (iCellsInARow - 1) / 2;                                       // Neighbours per direction.                E.g. 0,2,4,6,8...

                    // Iterate through all 3 axis, checking the centre cell first.
                    int x = 0;
                    int y = 0;
                    int z = 0;
                    do
                    {
                        do
                        {
                            do
                            {
                                // Check if this cell is loaded.
                                SGridCellPos neighbouringRelativeCell = new SGridCellPos(occupiedRelativeCell.x + x, occupiedRelativeCell.y + y, occupiedRelativeCell.z + z);
                                if (RelativeCellWithinProximityOfPoint(neighbouringRelativeCell, gubbinPosition, gubbin.mBoundingRadius))
                                {
                                    if (mGrid.ContainsKey(neighbouringRelativeCell))
                                    {
                                        gubbin.mAlternator = mbValidCellValue;
                                        x = y = z = -1;  // Way to break the nested loop.
                                    }
                                }

                                ++z; if (z > iNeighboursPerDirection) z = -iNeighboursPerDirection;
                            }while(z != 0);

                            ++y; if (y > iNeighboursPerDirection) y = -iNeighboursPerDirection;
                        }while(y != 0);

                        ++x; if (x > iNeighboursPerDirection) x = -iNeighboursPerDirection;
                    }while(x != 0);
                }
                Profiler.EndSample();

                // Unload gubbins that are not within proximity to the cells.
                Profiler.BeginSample("Unload gubbins");
                bool restart;
                do
                {
                    restart = false;
                    foreach (CRegisteredGubbin gubbin in mGubbins)
                    {
                        if (gubbin.mAlternator != mbValidCellValue)
                        {
                            UnloadGubbin(gubbin);
                            restart = true;
                            break;
                        }
                    }
                }
                while (restart);
                Profiler.EndSample();
            }

            // Shift the galaxy if necessary. Remainder is preserved so shifts take place regardless of lag spikes.
            mfTimeUntilNextShiftTest -= Time.deltaTime;
            if (mfTimeUntilNextShiftTest <= 0.0f)  // Running more than once per frame does nothing extra.
            {
                mfTimeUntilNextShiftTest = mfTimeBetweenShiftTests;   // Drop the remainder as it is not required to run multiple times to make up for lag spikes.

                // Shift the galaxy if the average position of all points is far from the centre of the scene (0,0,0).
                SGridCellPos relativeCentrePos = PointToRelativeCell(CalculateAverageObserverPosition());
                if (relativeCentrePos.x != 0)
                    mCentreCellX.Set(mCentreCell.x + relativeCentrePos.x);
                if (relativeCentrePos.y != 0)
                    mCentreCellY.Set(mCentreCell.y + relativeCentrePos.y);
                if (relativeCentrePos.z != 0)
                    mCentreCellZ.Set(mCentreCell.z + relativeCentrePos.z);
            }

            // Load queued cells over time. Remainder is preserved so cells are loaded regardless of lag spikes.
            mfTimeUntilNextCellLoad -= Time.deltaTime;
            while (mfTimeUntilNextCellLoad <= 0.0f) // May load more than one queued cell per frame.
            {
                mfTimeUntilNextCellLoad += mfTimeBetweenCellLoads;  // Preserve the remainder so the right number of cells are loaded over time, regardless of lag spikes.

                // Time to load a cell.
                if (mCellsToLoad.Count > 0)  // If there are cells to load...
                {
                    // Get the first cell to load.
                    SGridCellPos cellToLoad = mCellsToLoad.Dequeue();
                    LoadAbsoluteCell(cellToLoad);
                }
            }
        }
    }

    private void ShiftEntities(Vector3 translation)
    {
        Profiler.BeginSample("ShiftEntities");

        foreach (Transform transform in mShiftableTransforms)
            transform.position += translation;

        Profiler.EndSample();
    }

    public Vector3 CalculateAverageObserverPosition()
    {
        Profiler.BeginSample("CalculateAverageObserverPosition");

        Vector3 result = new Vector3();
        foreach (CRegisteredObserver observer in mObservers)
            result += observer.mEntity.transform.position;

        Profiler.EndSample();

        return result / mObservers.Count;
    }

    public void SyncNoiseSeed(INetworkVar sender)
    {
        for(uint ui = 0; ui < (uint)ENoiseLayer.MAX; ++ui)
            if(mNoiseSeeds[ui] == sender)
                mNoises[ui].Seed(mNoiseSeeds[ui].Get());
    }

    public void SyncCentreCellX(INetworkVar sender)
    {
        ShiftEntities(new Vector3((mCentreCell.x - mCentreCellX.Get()) * cellDiameter, 0.0f, 0.0f));
        mCentreCell.x = mCentreCellX.Get();
    }
    public void SyncCentreCellY(INetworkVar sender)
    {
        ShiftEntities(new Vector3(0.0f, (mCentreCell.y - mCentreCellY.Get()) * cellDiameter, 0.0f));
        mCentreCell.y = mCentreCellY.Get();
    }
    public void SyncCentreCellZ(INetworkVar sender)
    {
        ShiftEntities(new Vector3(0.0f, 0.0f, (mCentreCell.z - mCentreCellZ.Get()) * cellDiameter));
        mCentreCell.y = mCentreCellY.Get();
    }
    public void SyncGalaxySize(INetworkVar sender) { mfGalaxySize = mGalaxySize.Get(); }
    public void SyncMaxAsteroidsPerCell(INetworkVar sender) { muiMaxAsteroidsPerCell = mMaxAsteroidsPerCell.Get(); }
    public void SyncNumGridSubsets(INetworkVar sender) { muiNumGridSubsets = mNumGridSubsets.Get(); }

    public void RegisterObserver(GameObject observer, float boundingRadius)
    {
        Profiler.BeginSample("RegisterObserver");
        mObservers.Add(new CRegisteredObserver(observer, boundingRadius));
        Profiler.EndSample();
    }

    public void DeregisterObserver(GameObject observer)
    {
        Profiler.BeginSample("DeregisterObserver");

        foreach (CRegisteredObserver elem in mObservers)
        {
            if (elem.mEntity.GetInstanceID() == observer.GetInstanceID())
            {
                mObservers.Remove(elem);
                break;
            }
        }

        Profiler.EndSample();
    }

    // Returns false if the galaxy is not ready to update galaxyIEs.
    public bool RegisterGalaxyIE(GalaxyIE galaxyIE)
    {
        if (mGalaxyIEs != null) // Whether the galaxy is ready to update galaxyIEs or not is determined by whether the list is instantiated or not.
        {
            Profiler.BeginSample("RegisterGalaxyIE");

            mGalaxyIEs.Add(galaxyIE);

            // Provide initial assets.
            UpdateGalaxyIE(mCentreCell, galaxyIE);

            Profiler.EndSample();

            return true;
        }
        else
            return false;
    }

    public void DeregisterGalaxyIE(GalaxyIE galaxyIE)
    {
        Profiler.BeginSample("DeregisterGalaxyIE");
        mGalaxyIEs.Remove(galaxyIE);
        Profiler.EndSample();
    }

    public void RegisterShiftableEntity(Transform shiftableTransform)
    {
        Profiler.BeginSample("RegisterShiftableEntity");
        mShiftableTransforms.Add(shiftableTransform);
        Profiler.EndSample();
    }

    public void DeregisterShiftableEntity(Transform shiftableTransform)
    {
        Profiler.BeginSample("DeregisterShiftableEntity");
        mShiftableTransforms.Remove(shiftableTransform);
        Profiler.EndSample();
    }

    void QueueCellToLoad(SGridCellPos absoluteCell)
    {
        mCellsToLoad.Enqueue(absoluteCell);
        mGrid.Add(absoluteCell, new CGridCellContent(mbValidCellValue));
    }

    void LoadAbsoluteCell(SGridCellPos absoluteCell)
    {
        Profiler.BeginSample("LoadAbsoluteCell");

        Profiler.BeginSample("Push new cell to dictionary");
        // If the cell was queued for loading, it will already have an entry in the grid, but unlike Add(); the [] operator allows overwriting existing elements in the dictionary.
        mGrid[absoluteCell] = new CGridCellContent(mbValidCellValue, true); // Create cell with updated alternator to indicate cell is within proximity of observer.
        Profiler.EndSample();

        SGridCellPos relativeCell = absoluteCell - mCentreCell;

        // Load the content for the cell.
        //if (false)   // TODO: If the content for the cell is on file...
        //{
        //    // TODO: Load content from SQL.
        //}
        //else    // This cell is not on file, so it has not been visited...
        {
            // Generate the content in the cell.
            float fCellRadius = cellDiameter*0.5f;

            // 1) For asteroids.
            uint uiNumAsteroids = (uint)Mathf.RoundToInt(muiMaxAsteroidsPerCell * (0.5f + 0.5f * mNoises[(uint)ENoiseLayer.AsteroidDensity].Generate(absoluteCell.x, absoluteCell.y, absoluteCell.z))) /*Mathf.RoundToInt(PerlinSimplexNoise.(ENoiseLayer.AsteroidDensity, absoluteCell))*/;
            for (uint ui = 0; ui < uiNumAsteroids; ++ui)
            {
                Profiler.BeginSample("Create asteroid");

                ushort firstAsteroid = (ushort)CGame.ENetworkRegisteredPrefab.Asteroid_FIRST;
                ushort lastAstertoid = (ushort)CGame.ENetworkRegisteredPrefab.Asteroid_LAST;
                ushort randomRange = (ushort)Random.Range(0, (lastAstertoid+1) - firstAsteroid);

                Profiler.BeginSample("Call to CNetwork.Factory.CreateObject()");
                GameObject newAsteroid = CNetwork.Factory.CreateObject((ushort)(firstAsteroid + randomRange));
                Profiler.EndSample();

                float uniformScale;

                // Work out a position where the asteroid fits.
                bool bSpotNoGood;
                int iTries = 5;    // To prevent infinite loops.
                do
                {
                    Profiler.BeginSample("Scale, position, and test for initial collision");

                    newAsteroid.transform.position = RelativeCellCentrePoint(relativeCell) + new Vector3(Random.Range(-fCellRadius, fCellRadius), Random.Range(-fCellRadius, fCellRadius), Random.Range(-fCellRadius, fCellRadius));

                    uniformScale = Random.Range(10.0f, 150.0f);
                    newAsteroid.transform.localScale = new Vector3(uniformScale, uniformScale, uniformScale);

                    bSpotNoGood = false/*Physics.CheckSphere(newAsteroid.transform.position, newAsteroid.collider.bounds.extents.magnitude)*/;    // If no collision is detected, there is room for the asteroid, so stop searching and place it.

                    Profiler.EndSample();
                } while (--iTries != 0 && bSpotNoGood);

                if (bSpotNoGood)    // If the spot is no good...
                {
                    Profiler.BeginSample("Call to CNetwork.Factory.DestoryObject()");
                    CNetwork.Factory.DestoryObject(newAsteroid.GetComponent<CNetworkView>().ViewId);
                    Profiler.EndSample();
                }
                else    // Else the spot is good...
                {
                    Profiler.BeginSample("Rotation, parent, linear velocity, angular velocity, mass");
                    newAsteroid.transform.parent = gameObject.transform;
                    newAsteroid.transform.rotation = Random.rotationUniform;

                    newAsteroid.rigidbody.angularVelocity = Random.onUnitSphere * Random.Range(0.0f, 2.0f);
                    newAsteroid.rigidbody.velocity = Random.onUnitSphere * Random.Range(0.0f, 75.0f);

                    newAsteroid.rigidbody.mass *= uniformScale * uniformScale;  // Alter default mass to be proportionate to the new scale.

                    Profiler.EndSample();
                    Profiler.BeginSample("Network sync all asteroid variables");

                    newAsteroid.GetComponent<NetworkedEntity>().UpdateNetworkVars();
                    CNetworkView newAsteroidNetworkView = newAsteroid.GetComponent<CNetworkView>();
                    //newAsteroidNetworkView.SyncTransformPosition();
                    //newAsteroidNetworkView.SyncTransformRotation();
                    newAsteroidNetworkView.SyncTransformScale();
                    newAsteroidNetworkView.SyncRigidBodyMass();
                    newAsteroidNetworkView.SyncParent();

                    Profiler.EndSample();

                    Profiler.BeginSample("Push asteroid to dictionary");
                    mGubbins.Add(new CRegisteredGubbin(newAsteroid, newAsteroid.collider.bounds.extents.magnitude, newAsteroidNetworkView.ViewId, mbValidCellValue));  // Add new asteroid to list of gubbins ("space things").
                    Profiler.EndSample();
                }

                Profiler.EndSample();
            }
        }

        Profiler.EndSample();
    }

    void UnloadAbsoluteCell(SGridCellPos absoluteCell)
    {
        Profiler.BeginSample("UnloadAbsoluteCell");

        // Todo: Save stuff to file.
        mGrid.Remove(absoluteCell); // Unload the cell.

        Profiler.EndSample();
    }

    void UnloadGubbin(CRegisteredGubbin gubbin)
    {
        Profiler.BeginSample("UnloadGubbin");

        // Todo: Save gubbin to file.
        mGubbins.Remove(gubbin);
        CNetwork.Factory.DestoryObject(gubbin.mNetworkViewID);

        Profiler.EndSample();
    }

    public float SampleNoise(float x, float y, float z, ENoiseLayer noiseLayer)
    {
        return mNoises[(uint)noiseLayer].Generate(x, y, z);
    }

    public Vector3 RelativeCellCentrePoint(SGridCellPos relativeCell)
    {
        Profiler.BeginSample("RelativeCellCentrePoint");

        Vector3 result = new Vector3(relativeCell.x * cellDiameter, relativeCell.y * cellDiameter, relativeCell.z * cellDiameter);

        Profiler.EndSample();

        return result;
    }

    public SGridCellPos PointToAbsoluteCell(Vector3 point)
    {
        Profiler.BeginSample("PointToAbsoluteCell");

        float cellRadius = cellDiameter * 0.5f;
        point.x += cellRadius;
        point.y += cellRadius;
        point.z += cellRadius;
        point /= cellDiameter;
        SGridCellPos result = new SGridCellPos(Mathf.FloorToInt(point.x) + mCentreCell.x, Mathf.FloorToInt(point.y) + mCentreCell.y, Mathf.FloorToInt(point.z) + mCentreCell.z);

        Profiler.EndSample();

        return result;
    }

    public SGridCellPos PointToRelativeCell(Vector3 point)
    {
        Profiler.BeginSample("PointToRelativeCell");

        float cellRadius = cellDiameter * 0.5f;
        point.x += cellRadius;
        point.y += cellRadius;
        point.z += cellRadius;
        point /= cellDiameter;
        SGridCellPos result = new SGridCellPos(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y), Mathf.FloorToInt(point.z));

        Profiler.EndSample();

        return result;
    }

    public bool RelativeCellWithinProximityOfPoint(SGridCellPos relativeCell, Vector3 point, float pointRadius)
    {
        Profiler.BeginSample("RelativeCellWithinProximityOfPoint");

        Vector3 cellCentrePos = new Vector3(relativeCell.x * cellDiameter, relativeCell.y * cellDiameter, relativeCell.z * cellDiameter);
        float cellBoundingSphereRadius = cellDiameter * 0.86602540378443864676372317075294f;
        bool result = (cellCentrePos - point).sqrMagnitude <= cellBoundingSphereRadius * cellBoundingSphereRadius + pointRadius * pointRadius;

        Profiler.EndSample();

        return result;
    }

    // Set the aesthetic of the galaxy based on the observer's position.
    void UpdateGalaxyIE(SGridCellPos absoluteCell, GalaxyIE galaxyIE)
    {
        Profiler.BeginSample("UpdateGalaxyIE");

        // Skybox.
        galaxyIE.mSkyboxMaterial.SetTexture("_Skybox1", mSkyboxes[(uint)ESkybox.Composite]);
        galaxyIE.mSkyboxMaterial.SetVector("_Tint", Color.grey);

        // Fog.
        galaxyIE.mFogMaterial.SetFloat("_FogDensity", 0.001f);
        galaxyIE.mFogStartDistance = 2000.0f;

        Profiler.EndSample();
    }

    void OnDrawGizmos()/*OnDrawGizmos & OnDrawGizmosSelected*/
    {
        Profiler.BeginSample("OnDrawGizmos");

        GL.Color(Color.white);
        foreach (CRegisteredObserver elem in mObservers)
            Gizmos.DrawWireSphere(elem.mEntity.transform.position, elem.mBoundingRadius);

        GL.Color(Color.blue);
        foreach (CRegisteredGubbin elem in mGubbins)
            Gizmos.DrawWireSphere(elem.mEntity.transform.position, elem.mBoundingRadius);
        
        float fCellDiameter = cellDiameter;
        float fCellRadius = fCellDiameter * .5f;

        foreach (System.Collections.Generic.KeyValuePair<SGridCellPos, CGridCellContent> pair in mGrid)
        {
            SGridCellPos relativeCell = pair.Key - mCentreCell;

            float x = relativeCell.x * fCellDiameter;
            float y = relativeCell.y * fCellDiameter;
            float z = relativeCell.z * fCellDiameter;

            // Set colour based on whether it is loaded or waiting to load.
            GL.Color(pair.Value.mLoaded ? Color.red : Color.yellow);

            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y - fCellRadius, z - fCellRadius);
            GL.Vertex3(x + fCellRadius, y - fCellRadius, z - fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y + fCellRadius, z - fCellRadius);
            GL.Vertex3(x + fCellRadius, y + fCellRadius, z - fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y - fCellRadius, z + fCellRadius);
            GL.Vertex3(x + fCellRadius, y - fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y + fCellRadius, z + fCellRadius);
            GL.Vertex3(x + fCellRadius, y + fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y - fCellRadius, z - fCellRadius);
            GL.Vertex3(x - fCellRadius, y + fCellRadius, z - fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x + fCellRadius, y - fCellRadius, z - fCellRadius);
            GL.Vertex3(x + fCellRadius, y + fCellRadius, z - fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y - fCellRadius, z + fCellRadius);
            GL.Vertex3(x - fCellRadius, y + fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x + fCellRadius, y - fCellRadius, z + fCellRadius);
            GL.Vertex3(x + fCellRadius, y + fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y - fCellRadius, z - fCellRadius);
            GL.Vertex3(x - fCellRadius, y - fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x - fCellRadius, y + fCellRadius, z - fCellRadius);
            GL.Vertex3(x - fCellRadius, y + fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x + fCellRadius, y - fCellRadius, z - fCellRadius);
            GL.Vertex3(x + fCellRadius, y - fCellRadius, z + fCellRadius);
            GL.End();
            GL.Begin(GL.LINES);
            GL.Vertex3(x + fCellRadius, y + fCellRadius, z - fCellRadius);
            GL.Vertex3(x + fCellRadius, y + fCellRadius, z + fCellRadius);
            GL.End();
        }

        Profiler.EndSample();
    }
}