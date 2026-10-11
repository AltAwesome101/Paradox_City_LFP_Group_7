using System;
using JetBrains.Annotations;
using UnityEngine;

public abstract class BaseTutorialInstructionSO : ScriptableObject
{
    [TextArea] public String Instruction;
    public Texture2D InstructionIcon;
    public bool FreezeGame = true;
    public abstract IPredicate CreateCondition(InputReaderSO input);

}
