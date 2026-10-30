using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Keychain HUD: a wire loop with one circle per playable level.
/// Circle colour reflects LevelProgressManager.LevelState via USS classes.
/// Requires Unity 2022.1+ (Painter2D).
/// </summary>
public class KeyChainView : BaseUIView, IDisposable
{
    // Order here = order of keychain-circle-0/1/2 in the UXML.
    // Must match the scene names in LevelProgressManager.playableLevels.
    static readonly string[] LevelIDs =
    {
        "WrongBeerLevel",
        "Artist_Hitler_Level",
        "Forest_Scene"
    };

    // Indexed by (int)LevelProgressManager.LevelState
    static readonly string[] StateClasses =
    {
        "keychain-circle--unplayed",
        "keychain-circle--active",
        "keychain-circle--completed"
    };

    // Wire loop control points, normalised to the wire element (0..1)
    static readonly Vector2[] WirePoints =
    {
        new(0.40f, 0.04f), new(0.72f, 0.30f), new(0.97f, 0.72f), new(0.80f, 0.98f),
        new(0.40f, 0.88f), new(0.10f, 0.98f), new(0.02f, 0.62f), new(0.18f, 0.30f)
    };

    readonly VisualElement[] _circles = new VisualElement[LevelIDs.Length];
    readonly Color[] _confirmationColours = new Color[LevelIDs.Length];
    readonly VisualElement _wire;
    readonly LevelProgressManager _injected;

    // The manager may not exist yet when UIManager.Awake runs, so resolve lazily.
    LevelProgressManager Progress => _injected != null ? _injected : LevelProgressManager.Instance;

    public KeyChainView(VisualElement container, LevelProgressManager level = null) : base(container)
    {
        _injected = level;

        _wire = container.Q<VisualElement>("keychain-wire");
        for (int i = 0; i < _circles.Length; i++)
            _circles[i] = container.Q<VisualElement>($"keychain-circle-{i}");

        // _wire.generateVisualContent += DrawWire;
        _wire.RegisterCallback<GeometryChangedEvent>(_ => _wire.MarkDirtyRepaint());

        LevelProgressManager.OnLevelProgressChanged += OnProgressChanged;
        Refresh();
    }

    public void Dispose()
    {
        LevelProgressManager.OnLevelProgressChanged -= OnProgressChanged;
        // _wire.generateVisualContent -= DrawWire;
    }

    public override void Show()
    {
        base.Show();
        Refresh(); // catch anything that changed while hidden
    }

    void OnProgressChanged(string levelID) => Refresh();

    void Refresh()
    {
        var progress = Progress;

        for (int i = 0; i < _circles.Length; i++)
        {
            var circle = _circles[i];
            if (circle == null) continue;

            foreach (var cls in StateClasses) circle.RemoveFromClassList(cls);

            var state = progress != null
                ? progress.GetLevelState(LevelIDs[i])
                : LevelProgressManager.LevelState.Unplayed;

            int idx = Mathf.Clamp((int)state, 0, StateClasses.Length - 1);
            circle.AddToClassList(StateClasses[idx]);
        }
    }

    // void DrawWire(MeshGenerationContext mgc)
    // {
    //     var r = _wire.contentRect;
    //     if (r.width < 1f || r.height < 1f) return;

    //     var p = mgc.painter2D;
    //     int n = WirePoints.Length;

    //     Vector2 ToPx(Vector2 v) => new(v.x * r.width, v.y * r.height);
    //     Vector2 Mid(Vector2 a, Vector2 b) => (a + b) * 0.5f;

    //     p.lineWidth = 4f;
    //     p.strokeColor = Color.black;
    //     p.lineCap = LineCap.Round;
    //     p.lineJoin = LineJoin.Round;

    //     // Smooth closed loop: quadratic curves through segment midpoints
    //     p.BeginPath();
    //     p.MoveTo(Mid(ToPx(WirePoints[n - 1]), ToPx(WirePoints[0])));
    //     for (int i = 0; i < n; i++)
    //     {
    //         var ctrl = ToPx(WirePoints[i]);
    //         var end = Mid(ctrl, ToPx(WirePoints[(i + 1) % n]));
    //         p.QuadraticCurveTo(ctrl, end);
    //     }
    //     p.ClosePath();
    //     p.Stroke();
    // }
}