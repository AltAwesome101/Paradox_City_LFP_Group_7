using UnityEngine;

namespace Consequence
{
    public class ArtistConsequence : ITimelineConsequence
    {
        GameObject _go;
        public ArtistConsequence(GameObject go) => _go = go;

        public void Activate(){}
        public void Deactivate(){}

        public void OnObjectComplete()=> _go.SetActive(false);
        public void Propogate(float deltaTime){}
    }
    
}
