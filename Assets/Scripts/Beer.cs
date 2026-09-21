using UnityEngine;

public class Beer : MonoBehaviour
{
    [Header("Beer Information")]
    public string beerName = "Beer";

    public bool isWrongBeer = false;

    public bool IsWrongBeer()
    {
        return isWrongBeer;
    }
}