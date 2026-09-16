using UnityEngine;

public class Beer : MonoBehaviour
{
    public string beerName = "Beer";

    public bool isWrongBeer = false;

    public bool IsWrongBeer()
    {
        return isWrongBeer;
    }
}