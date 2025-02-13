using System;
using System.ComponentModel;
using AI;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Serialization;

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
        public bool canSeePlayer;
        public bool canHearPlayer;
        public Vector3 lastHeardPosition;
    }

    #region AI State

    // --------------------------------------------------
    private IState[] states = new IState[3];
    public State _currentState = State.Patrol;

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
    public float TimeBeforeAlert => timeBeforeAlert;
    
    // --------------------------------------------------

    #endregion

    #region Internal State

    // --------------------------------------------------
    private PlayerController _playerController;
    private NavMeshAgent _navMeshAgent;
    public NavMeshAgent NavMeshAgent => _navMeshAgent;
    // --------------------------------------------------

    #endregion

    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        InitState();
        _playerController = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        UpdatePerceptions();

        // Execute current state
        states[(uint)_currentState].Execute(this);
   }

    public void ChangeState(State newState) => _currentState = newState;

    private void UpdatePerceptions()
    {
        _perceptions.canSeePlayer = CanSeePlayer();
        _perceptions.canHearPlayer = CanHearPlayer();
        
        if (_perceptions.canHearPlayer)
            _perceptions.lastHeardPosition = _playerController.transform.position;
        
        if (_perceptions.canSeePlayer)
            Debug.Log("Seeing Player!");
    }

    private void InitState()
    {
        _currentState = State.Patrol;
        states[(uint)State.Patrol] = new Patrol();
        states[(uint)State.Alert] = new Alert();
        states[(uint)State.Chase] = new Chase();
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
        toPlayer.Normalize();
        bool hitSomething = Physics.Raycast(
            transform.position, 
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
    }
}