using System;
using System.Collections.Generic;
using UnityEngine;

public interface IDeployModule { void Enable(); void Disable(); }

[Serializable]
public class DeploySettings
{
    public int PoolCapacity = 10;
    public int MaxConcurrent = 2;
    public float MinSpawnInterval = 1.5f;
    public float WarningDuration = 3f;
}

public class AppleDeployManager : MonoBehaviour   // no more Singleton
{
    [SerializeField] InputReaderSO input;
    [SerializeField] UIManager ui;
    [SerializeField] DeploySettings settings;

    readonly List<IDeployModule> modules = new();
    DeploySessionController session;

    void Awake()   // build once; pools live as long as the scene
    {
        var spawner = new AppleSpawner(settings.PoolCapacity);
        session = new DeploySessionController(spawner, settings);

        modules.Add(session);
        modules.Add(new DeployZonePrompt(input));
        modules.Add(new DeploySessionDirector());
        modules.Add(new DeployHudPresenter(ui));
    }

    void OnEnable()  { foreach (var m in modules) m.Enable(); }
    void OnDisable() { foreach (var m in modules) m.Disable(); }
    void Update()=> session.Tick(Time.deltaTime);
}