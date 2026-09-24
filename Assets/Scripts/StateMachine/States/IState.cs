using UnityEngine;

public interface IState
{
    void OnEnter();
    void Update();
    void OnExit();
    void FixedUpdate();

}
