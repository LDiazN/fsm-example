using System.ComponentModel;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace AI
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class EnemyAgent : MonoBehaviour
    {
        public enum State
        {
            Patrol = 0,
            Alert = 1,
            Chase = 2
        }

        public struct Perceptions
        {
            public bool CanSeePlayer;
            public bool CanHearPlayer;
            public Vector3 LastKnownPosition;
        }

        #region AI State

        // --------------------------------------------------
        readonly IState[] _states = new IState[3];
        [FormerlySerializedAs("_currentState")] public State currentState = State.Patrol;

        private Perceptions _perceptions;

        public Perceptions GetPerceptions() => _perceptions;
        // --------------------------------------------------

        #endregion

        #region Inspector Properties

        // --------------------------------------------------
        [Header("Perceptions")]
        [Description("How far can this enemy look for the player. Depicted by red Gizmo")]
        [SerializeField]
        private float lookDistance = 2f;

        [Description("Field of view in degrees. Depicted by green Gizmo")] [SerializeField]
        private float visionAngle = 90;

        [Description("How far can this enemy hear the player. Depicted by yellow Gizmo")] [SerializeField]
        private float hearDistance = 1.5f;
    
        [Header("Patrol")] 
        [SerializeField] private List<Transform> waypoints;
        public List<Transform> Waypoints => waypoints;

        [Description("Time (in seconds) to wait in each waypoint before going to the next")]
        [SerializeField] private float timeToWait = 2;
        public float TimeToWait => timeToWait;

        [Description("How near the agent should be to the waypoint to consider it within the waypoint")]
        [SerializeField] private float toleranceDistance = 0.1f;
        public float ToleranceDistance => toleranceDistance;

        [Header("Alert")] 
        [Description("Time to wait when NOT hearing the player before changing to patrol again")]
        [SerializeField] private float timeBeforePatrol = 3;
        public float TimeBeforePatrol => timeBeforePatrol;
    
        [Header("Chase")]
        [Description("Time to wait without seeing the character before changing to alert again")]
        [SerializeField] private float timeBeforeAlert = 3;

        [Description("If the distance between the enemy and the last known position of the player is less than this, the enemy will stop moving")]
        [SerializeField] private float toleranceToLastPosition = 0.1f;
        public float TimeBeforeAlert => timeBeforeAlert;
    
        [Header("Debugging")]
    
        [Description("If the bunny is allowed to see the player, useful for debugging")]
        [SerializeField]
        private bool canSeePlayer = true;
    
        [Description("If the bunny is allowed to hear the player, useful for debugging")]
        [SerializeField]
        private bool canHearPlayer = true;
    
        [Description("If this enemy is allowed to finish the game catching the player. Useful for debugging")]
        [SerializeField] private bool canFinishGame = true;
    
        // --------------------------------------------------

        #endregion

        #region Internal State

        // --------------------------------------------------
        /// <summary>
        /// Player controller component attached to the player.
        /// Do not assume the player exists in scene, this component
        /// can be null
        /// </summary>
        private PlayerController _playerController;
        public GameObject Player => _playerController.gameObject;
        private NavMeshAgent _navMeshAgent;
        public NavMeshAgent NavMeshAgent => _navMeshAgent;
    
        // Used to get the original look at rotation 
        // when coming back from a chase.
        // Useful for enemies with 0 waypoints (watchers)
        private Quaternion _originalRotation;
    
        public Quaternion OriginalRotation => _originalRotation;
    
        private CapsuleCollider _capsuleCollider;
        public CapsuleCollider CapsuleCollider => _capsuleCollider;
        // --------------------------------------------------

        #endregion

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }

        void Start()
        {
            InitStates();
            _playerController = FindObjectOfType<PlayerController>();
        
            _originalRotation = transform.rotation;
        
            // If no waypoints are provided, consider your initial position as a waypoint.
            // That will help you generalize behaviour
            if (waypoints.Count == 0)
            {
                var newWaypoint = new GameObject($"${this.gameObject.name}__point");
                newWaypoint.transform.position = transform.position;
                waypoints.Add(newWaypoint.transform);
            }
        }

        void Update()
        {
            UpdatePerceptions();

            // Execute current state
            _states[(uint)currentState].Execute(this);
        }

        public void ChangeState(State newState) => currentState = newState;

        private void UpdatePerceptions()
        {
            _perceptions.CanSeePlayer = CanSeePlayer() && canSeePlayer;
            _perceptions.CanHearPlayer = CanHearPlayer() && canHearPlayer;
        
        
            if (_perceptions.CanSeePlayer || _perceptions.CanHearPlayer)
                _perceptions.LastKnownPosition = _playerController.transform.position;
        }

        private void InitStates()
        {
            currentState = State.Patrol;
            _states[(uint)State.Patrol] = new Patrol();
            _states[(uint)State.Alert] = new Alert();
            _states[(uint)State.Chase] = new Chase();
        }

        private bool CanSeePlayer()
        {
            // If no player in scene, just return false
            if (!_playerController)
                return false;
            
            // Check if the player is within the looking distance
            var playerPosition = _playerController.transform.position;
            var toPlayer = playerPosition - transform.position;

            // If too far, just return false without checking anything else
            if (toPlayer.sqrMagnitude > lookDistance * lookDistance)
                return false;

            // if outside FOV, just return false
            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > visionAngle * 0.5f)
                return false;

            // Do the raycast for visibility at the end only if it passes every other check
            // TODO run several raycasts in arc to handle the almost-visible case
        
            // There is a weird bug where the raycast starts in the floor, and sometimes
            // it detects the floor before the player. 
            // We solve this by casting a ray from the center of the collider
            toPlayer.Normalize();
            var colliderCenter = transform.position + _capsuleCollider.center;
            Debug.DrawRay(colliderCenter, toPlayer, Color.cyan);
            bool hitSomething = Physics.Raycast(
                colliderCenter, 
                toPlayer, 
                out RaycastHit hit, 
                lookDistance
            );

            // If didn't hit anything, didn't see the player
            if (!hitSomething)
                return false;
        
            // If hit something, check that's the player 
            var playerController = hit.collider.GetComponent<PlayerController>();
            return playerController != null;
        }

        private bool CanHearPlayer()
        {
            Vector3 playerPosition = _playerController.transform.position;
            var toPlayer = playerPosition - transform.position;
        
            // If too far, return false
            if (toPlayer.sqrMagnitude > hearDistance * hearDistance)
                return false;

            // Player only makes noise when moving
            return _playerController.IsWalking();
        }

        private void OnDrawGizmos()
        {
            // Draw the forward vector
            Gizmos.color = Color.blue; // Same color as Z, since forward is +z
            Gizmos.DrawRay(transform.position, transform.forward * 1);

            // Draw look distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, lookDistance);

            // Draw hear distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, hearDistance);

            // Draw field of view
            float halfAngle = visionAngle * 0.5f;
            Gizmos.color = Color.green;
            Vector3 rightIsh = Quaternion.AngleAxis(halfAngle, Vector3.up) * transform.forward;
            Vector3 leftIsh = Quaternion.AngleAxis(-halfAngle, Vector3.up) * transform.forward;
            Gizmos.DrawRay(transform.position, rightIsh * 1.5f * lookDistance);
            Gizmos.DrawRay(transform.position, leftIsh * 1.5f * lookDistance);


            if (_playerController)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_perceptions.LastKnownPosition, 0.5f);
            }
        }

        private void CatchPlayer()
        {
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
                return;
        
            gameManager.GameOver();
        }

        void OnCollisionEnter(Collision collision)
        {
            var playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController == null)
                return;
        
            CatchPlayer();
        }
    }
}