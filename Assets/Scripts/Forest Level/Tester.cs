using UnityEngine;
using System;

public class Tester : MonoBehaviour
{
    [SerializeField] InputReaderSO input;

    void Start()=>input.EnableInputMap();
    void FixedUpdate()
    {
        DisplayNonZeroInput();
    }
    void DisplayNonZeroInput(){
        if(input.MoveDirection != Vector2.zero)
            Debug.Log($"inputMove: {input.MoveDirection}");
    }

}
