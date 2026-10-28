using Unity.AppUI.MVVM;
using UnityEngine;

public class AppleVisualRenderer
{
    // Renders the apple and makes it look nice
    // Data driven and depends on the material or etc...
    readonly Renderer[] _renderers;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    // static readonly int HueID = Shader.PropertyToID("_Hue");
    // static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
    readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

    public AppleVisualRenderer(Renderer[] renderers){
        _renderers = renderers;
    }

    public void Apply(Color color){
        foreach(var rend in _renderers){
            rend.GetPropertyBlock(_block);
            _block.SetColor(BaseColorID, color);
            rend.SetPropertyBlock(_block);
        }
    }
}
