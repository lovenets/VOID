﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CNetworkView))]
[RequireComponent(typeof(ParticleEmitter))]
public class GalaxyProjector : CNetworkMonoBehaviour
{
    public CGalaxy.SCellPos centreCellOfProjection { get { return centreCellOfProjection_internal; } set { centreCellOfProjectionX.Set(value.x); centreCellOfProjectionY.Set(value.y); centreCellOfProjectionZ.Set(value.z); } }
    private CGalaxy.SCellPos centreCellOfProjection_internal = new CGalaxy.SCellPos(0, 0, 0);
    private CNetworkVar<int> centreCellOfProjectionX;
    private CNetworkVar<int> centreCellOfProjectionY;
    private CNetworkVar<int> centreCellOfProjectionZ;

    // Radius.
    [SerializeField] private float initialRadius = 3.0f;
    private CNetworkVar<float> radius_internal;
    [HideInInspector] public float radius { get { return radius_internal.Get(); } set { radius_internal.Set(value); } }

    // Zoom.
    [SerializeField] private float initialZoom = 1.0f;
    private CNetworkVar<float> zoom_internal;   // 1 = Maximum zoom out (-1 to +1). 0 = infinite zoom.
    [HideInInspector] public float zoom { get { return zoom_internal.Get(); } set { zoom_internal.Set(value); } }

    public int samplesPerAxis = 28; // 25 with particle scale 2.5f
    public float particleScale = 2.5f;  // This changes how much each particle overlaps neighbouring particles.

    public float framesPerSecond = 10;
    private float timeOfNextUpdate = 0.0f;

    private bool mUpToDate = false;

    public override void InstanceNetworkVars()
    {
        radius_internal = new CNetworkVar<float>(SyncNetworkVar, initialRadius);
        zoom_internal = new CNetworkVar<float>(SyncNetworkVar, initialZoom);
        centreCellOfProjectionX = new CNetworkVar<int>(SyncNetworkVar, centreCellOfProjection_internal.x);
        centreCellOfProjectionY = new CNetworkVar<int>(SyncNetworkVar, centreCellOfProjection_internal.y);
        centreCellOfProjectionZ = new CNetworkVar<int>(SyncNetworkVar, centreCellOfProjection_internal.z);
    }

    void SyncNetworkVar(INetworkVar sender)
    {
        mUpToDate = false;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //zoom = 0.01f + 1.5f + Mathf.Cos(Mathf.PingPong(Time.time*0.25f, Mathf.PI)) * 1.5f;
        //mUpToDate = false;

        if (!mUpToDate && timeOfNextUpdate <= Time.time)
        {
            timeOfNextUpdate = framesPerSecond > 0.0f ? Time.time + (1.0f / framesPerSecond) : float.PositiveInfinity;
            RefreshProjection();
        }
    }

    void RefreshProjection()
    {
        Profiler.BeginSample("RefreshProjection");
        ParticleEmitter emitter = GetComponent<ParticleEmitter>();
        emitter.ClearParticles();

        CGalaxy galaxy = CGalaxy.instance;
        if (galaxy && samplesPerAxis > 0)
        {
            Vector3 centreSample = new Vector3((samplesPerAxis - 1) * 0.5f, (samplesPerAxis - 1) * 0.5f, (samplesPerAxis - 1) * 0.5f);

            for (int x = 0; x < samplesPerAxis; ++x)
                for (int y = 0; y < samplesPerAxis; ++y)
                    for (int z = 0; z < samplesPerAxis; ++z)
                    {
                        Vector3 unitPos = new Vector3(x - centreSample.x, y - centreSample.y, z - centreSample.z) / (0.5f * samplesPerAxis);    // -1 to +1 on each axis.
                        Vector3 centreOfProjectionSamplePos = galaxy.AbsoluteCellNoiseSamplePoint(centreCellOfProjection);
                        float asteroidDensity = galaxy.SampleNoise(centreOfProjectionSamplePos.x + unitPos.x /** zoom*/, centreOfProjectionSamplePos.y + unitPos.y /** zoom*/, centreOfProjectionSamplePos.z + unitPos.z /** zoom*/, CGalaxy.ENoiseLayer.SparseAsteroidCount);
                        float asteroidDensityAlpha = asteroidDensity * asteroidDensity * asteroidDensity * asteroidDensity;
                        asteroidDensityAlpha = (1.0f - unitPos.sqrMagnitude) * asteroidDensityAlpha;
                        //if (asteroidDensityAlpha < 0.0f)
                        //    asteroidDensityAlpha = 0.0f;

                        if (asteroidDensityAlpha >= 0.0625f)
                            emitter.Emit(unitPos * radius, Vector3.zero, particleScale * (radius * 2) / samplesPerAxis, float.PositiveInfinity, new Color(0.5f, 0.5f, 0.75f, asteroidDensityAlpha));
                    }

            mUpToDate = true;

        }

        Profiler.EndSample();
    }
}