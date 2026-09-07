using System.Collections.Generic;
using UnityEngine;


public class PaintingStatusIndicator : MonoBehaviour
{
    [Header("Source")]
    public PaintingCanvas painting;

    [Header("Placement")]
    [Tooltip("Local offset above the canvas the icon is drawn at.")]
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("World-space size of the icon.")]
    public float iconSize = 0.35f;

    [Header("Indicator Rotation")]
    [Tooltip("Rotation of the entire status indicator.")]
    public Vector3 indicatorRotationEuler = Vector3.zero;

    [Header("Visuals")]
    public Color crossColor = new Color(0.9f, 0.15f, 0.15f);
    public Color tickColor = new Color(0.2f, 0.85f, 0.3f);

    public float particleSize = 0.05f;

    [Tooltip("Points sampled along each stroke - higher = smoother icon, more particles.")]
    public int pointsPerStroke = 10;

    [Tooltip("How much the icon gently pulses (fraction of particleSize).")]
    public float pulseAmount = 0.15f;

    public float pulseSpeed = 2f;

    [Header("Rotation")]
    [Tooltip("If true, the icon always faces the camera.")]
    public bool billboardToCamera = true;

    [Tooltip("Only used when billboardToCamera is off.")]
    public Vector3 fixedRotationEuler = Vector3.zero;

    [Tooltip("Rotates the tick/cross shape itself around its own center, in degrees.")]
    public float shapeRotationZ = 0f;

    [Header("Completion Pop")]
    public bool playPopOnComplete = true;
    public int popParticleCount = 30;

    [Header("Debug")]
    public bool debugLogging = false;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] iconParticles;
    private bool lastComplete;
    private bool initialized;

    private void Start()
    {
        if (painting == null)
        {
            Debug.LogWarning(
                "[PaintingStatusIndicator] No PaintingCanvas assigned - nothing to show."
            );

            enabled = false;
            return;
        }

        BuildParticleSystem();

        lastComplete = painting.IsComplete;

        SetIcon(lastComplete);

        initialized = true;
    }

    private void BuildParticleSystem()
    {
        GameObject psObj = new GameObject($"{painting.name}_StatusIcon");

        psObj.transform.SetParent(painting.transform, false);

        psObj.transform.localPosition = offset;

        
        psObj.transform.localRotation =
            Quaternion.Euler(indicatorRotationEuler);

        ps = psObj.AddComponent<ParticleSystem>();

        ps.Stop();

        var main = ps.main;
        main.loop = false;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize = particleSize;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 256;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        ApplyFallbackShader(
            psObj.GetComponent<ParticleSystemRenderer>()
        );

        ParticleSystemRenderer psRenderer =
            psObj.GetComponent<ParticleSystemRenderer>();

        psRenderer.alignment =
            billboardToCamera
                ? ParticleSystemRenderSpace.Facing
                : ParticleSystemRenderSpace.Local;

        
        if (!billboardToCamera)
        {
            psObj.transform.localRotation =
                Quaternion.Euler(fixedRotationEuler);
        }

        ps.Play();
    }

    private void Update()
    {
        if (!initialized || painting == null || ps == null)
            return;

        
        ps.transform.localRotation =
            Quaternion.Euler(
                billboardToCamera
                    ? indicatorRotationEuler
                    : fixedRotationEuler
            );

        bool complete = painting.IsComplete;

        if (complete != lastComplete)
        {
            lastComplete = complete;

            SetIcon(complete);

            if (debugLogging)
            {
                Debug.Log(
                    $"[PaintingStatusIndicator] {painting.name} -> " +
                    $"{(complete ? "TICK" : "CROSS")}"
                );
            }

            if (complete && playPopOnComplete)
                PlayCompletionPop();
        }

        if (iconParticles != null && iconParticles.Length > 0)
        {
            float pulse =
                1f +
                Mathf.Sin(Time.time * pulseSpeed) *
                pulseAmount;

            for (int i = 0; i < iconParticles.Length; i++)
            {
                iconParticles[i].startSize =
                    particleSize * pulse;
            }

            ps.SetParticles(
                iconParticles,
                iconParticles.Length
            );
        }
    }

    private void SetIcon(bool complete)
    {
        Vector2[] shapePoints =
            complete
                ? BuildTickPoints()
                : BuildCrossPoints();

        Color color =
            complete
                ? tickColor
                : crossColor;

        iconParticles =
            new ParticleSystem.Particle[shapePoints.Length];

        for (int i = 0; i < shapePoints.Length; i++)
        {
            Vector2 point = shapePoints[i];

            
            if (shapeRotationZ != 0f)
            {
                float radians =
                    shapeRotationZ * Mathf.Deg2Rad;

                float cos = Mathf.Cos(radians);
                float sin = Mathf.Sin(radians);

                float x =
                    point.x * cos -
                    point.y * sin;

                float y =
                    point.x * sin +
                    point.y * cos;

                point = new Vector2(x, y);
            }

            iconParticles[i] =
                new ParticleSystem.Particle
                {
                    position =
                        new Vector3(
                            point.x,
                            point.y,
                            0f
                        ) * iconSize,

                    startColor = color,

                    startSize = particleSize,

                    startLifetime = Mathf.Infinity,

                    remainingLifetime = Mathf.Infinity,

                    velocity = Vector3.zero,

                    rotation = 0f
                };
        }

        ps.SetParticles(
            iconParticles,
            iconParticles.Length
        );
    }

    
    private Vector2[] BuildCrossPoints()
    {
        var points = new List<Vector2>();

        AddLine(
            points,
            new Vector2(-0.5f, -0.5f),
            new Vector2(0.5f, 0.5f),
            pointsPerStroke
        );

        AddLine(
            points,
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, -0.5f),
            pointsPerStroke
        );

        return points.ToArray();
    }

    
    private Vector2[] BuildTickPoints()
    {
        var points = new List<Vector2>();

        AddLine(
            points,
            new Vector2(-0.45f, 0f),
            new Vector2(-0.1f, -0.4f),
            Mathf.Max(2, pointsPerStroke / 2)
        );

        AddLine(
            points,
            new Vector2(-0.1f, -0.4f),
            new Vector2(0.5f, 0.45f),
            pointsPerStroke
        );

        return points.ToArray();
    }

    private void AddLine(
        List<Vector2> points,
        Vector2 a,
        Vector2 b,
        int count
    )
    {
        count = Mathf.Max(2, count);

        for (int i = 0; i < count; i++)
        {
            float t =
                i / (float)(count - 1);

            points.Add(
                Vector2.Lerp(a, b, t)
            );
        }
    }

    
    private void PlayCompletionPop()
    {
        GameObject popObj =
            new GameObject("StatusPop");

        popObj.transform.position =
            ps.transform.position;

        ParticleSystem pop =
            popObj.AddComponent<ParticleSystem>();

        pop.Stop();

        var main = pop.main;

        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.startSize = particleSize * 1.5f;
        main.startColor = tickColor;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = pop.emission;

        emission.rateOverTime = 0f;

        emission.SetBursts(
            new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(
                    0f,
                    (short)popParticleCount
                )
            }
        );

        var shape = pop.shape;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;

        shape.radius = 0.2f;

        ApplyFallbackShader(
            popObj.GetComponent<ParticleSystemRenderer>()
        );

        pop.Play();

        Destroy(
            popObj,
            main.duration +
            main.startLifetime.constantMax +
            0.5f
        );
    }

    
    private static void ApplyFallbackShader(
        ParticleSystemRenderer renderer
    )
    {
        if (renderer == null)
            return;

        Shader shader =
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find(
                "Universal Render Pipeline/Particles/Unlit"
            ) ??
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            renderer.material =
                new Material(shader);
        }
    }
}