using UnityEngine;

public readonly  struct AppleContext
{
    public readonly Apple Apple;
    public readonly Vector3 Position;
    public AppleContext(Apple apple, Vector3 position){
        Apple = apple;
        Position = position;
    }
}
