using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorController : MonoBehaviour
{
    [SerializeField]Animator _anim;
    [SerializeField] float movementDampTime = .2f;

    //Animation constant hashes
    static readonly int MovementValueHash = Animator.StringToHash("movementValue");
    static readonly int onSurfaceHash = Animator.StringToHash("onSurface");
    static readonly int HandsUpHash = Animator.StringToHash("HandsUp");

    // Public fields
    public Animator Animator => _anim;


    void Awake(){
        if(_anim == null)
            _anim = GetComponent<Animator>();
    }

    public void SetMovement(float amount) => _anim.SetFloat(MovementValueHash,amount, movementDampTime, Time.deltaTime);
    public void StopMovement()=> _anim.SetFloat(MovementValueHash,0f);
    public void SetOnSurface(bool onSurface)=> _anim.SetBool(onSurfaceHash, onSurface);
    public void SetHandsUp(bool handsUp) => _anim.SetBool(HandsUpHash, handsUp);
}
