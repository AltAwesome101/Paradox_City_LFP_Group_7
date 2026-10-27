using Unity.AppUI.MVVM;
using UnityEngine;

public class AppleVisualRenderer
{
    // Renders the apple and makes it look nice
    // Data driven and depends on the material or etc...

    readonly Renderer _renderer;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    // static readonly int HueID = Shader.PropertyToID("_Hue");
    // static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
    readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

    public AppleVisualRenderer(Renderer renderer){
        _renderer = renderer;
    }

    public void Apply(Color color){
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorID, color); //set colour
        // _block.SetFloat(HueID,hue); //set hue
        // _block.SetFloat(OutlineWidthID, outline); //set outline
        _renderer.SetPropertyBlock(_block);
    }
}
