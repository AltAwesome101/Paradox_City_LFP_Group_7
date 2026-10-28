using UnityEngine;

public class AppleScaler
{
    readonly Transform transform;
    public AppleScaler(Transform transform){
        this.transform = transform;
    }

    public void Scale(float scale){
        transform.localScale = new Vector3(scale,scale,scale);
    }
}
