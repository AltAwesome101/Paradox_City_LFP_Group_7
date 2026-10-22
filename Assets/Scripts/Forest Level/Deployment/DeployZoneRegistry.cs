using GameRegistry;
using UnityEngine;

[CreateAssetMenu(fileName = "DeployZoneRegistry", menuName = "Registries/Deploy Zone Registry")]
public class DeployZoneRegistry : Registry<DeployZone> {
    public int Total => _items.Count;
    public bool IsCompleted => Total > 0 && CompleteCount ==Total;
    public int CompleteCount
    {
        get
        {
            int count = 0;
            foreach(var zone in _items){
                if (zone.IsCompleted)
                    count++;
            }

            return count;
        }
    }

    public DeployZone NearestIncomplete(Vector3 from)
    {
        DeployZone best = null; float bestSqr = float.MaxValue;
        foreach (var zone in _items)
        {
            if (zone.IsCompleted) continue;
            float sqr = (zone.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr) { 
                bestSqr = sqr; 
                best = zone; 
            }
        }
       return best;   // null when everything is done
    }
}
