using UnityEngine;
using GameRegistry;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DeployRegistry", menuName = "Registries/Deploy Registry")]
public class DeployRegistry : Registry<AppleDeployer>
{
    public int DeployCount => _items.Count;
    public int BusyCount
    {
        get
        {
            int count = 0;
            foreach (var item in _items){
                if(item.IsBusy)
                    count++;
            }

            return count;
        }
    }

    public IEnumerable<AppleDeployer> GetAllFree(){
        foreach(var item in _items)
        {
            if(!item.IsBusy)
                yield return item; //! expected Deploy issue 
        }
    }
}
