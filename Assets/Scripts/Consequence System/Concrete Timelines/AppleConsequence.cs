using UnityEngine;

namespace Consequence
{
    public class AppleConsequence : ITimelineConsequence
    {
        Rigidbody _rb;

        public AppleConsequence(Rigidbody rb){
            _rb = rb;
        }


        public void Activate(){}
        public void Deactivate(){}

        public void OnObjectComplete(){}

        public void Propogate(float deltaTime)
        {
            //Randomly float in random directions. can interact with other objects to push it away much like a vaccum in space
        }
    }

}
