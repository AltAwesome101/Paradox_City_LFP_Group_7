using UnityEngine;

namespace Forestlevel
{
    /// <summary>
    /// Character controller movement, gravity, ground checks, rotation and teleportation
    /// </summary>
    public class PlayerMotion : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] CharacterController character;

        [Header("Movement")]
        [SerializeField] float movementSpeed = 5f;
        [SerializeField] float rotationSpeed = 450f;

        [Header("Ground Check")]
        [SerializeField] float surfaceCheckRadius = .1f;
        [SerializeField] Vector3 surfaceCheckOffset;
        [SerializeField] LayerMask surfaceLayer;

        const float GroundedStickSpeed = -.5f;

        Quaternion targetRotation;
        float fallingSpeed;

        // public fields
        public CharacterController CharacterController => character;
        public float MovementSpeed => movementSpeed;
        public bool OnSurface {get; private set;}
        public Vector3 HorizontalVelocity {get; private set;}

        void Awake(){
            if(character == null)
                character = GetComponent<CharacterController>();
            
            targetRotation = transform.rotation;
        }

        public void RefreshSurface()=> OnSurface = Physics.CheckSphere(transform.TransformPoint(surfaceCheckOffset),surfaceCheckRadius,surfaceLayer);
        public void HoldCurrentRotation() => targetRotation = transform.rotation;
        public void SetEnabled(bool enabled) => character.enabled = enabled;

        public void Move(Vector3 desiredHorizontal, Vector3 facing, float deltaTime){

            if(OnSurface){
                fallingSpeed = GroundedStickSpeed;
                HorizontalVelocity = new Vector3(desiredHorizontal.x, 0f, desiredHorizontal.z);
            }
            else
                fallingSpeed += Physics.gravity.y *deltaTime;
        
            var velocity = HorizontalVelocity;
            velocity.y = fallingSpeed;

            if(character.enabled)
                character.Move(velocity * deltaTime);
            
            if(facing.sqrMagnitude > .0001f)
                targetRotation = Quaternion.LookRotation(facing);
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,rotationSpeed * deltaTime);
        }

        public void Teleport(Vector3 destination)
        {
            var wasEnabled = character.enabled;
            character.enabled = false;
            transform.position = destination;
            character.enabled = wasEnabled;

            // Reset character motion
            fallingSpeed = 0f;
            HorizontalVelocity = Vector3.zero;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadius);
        }
    }
}

