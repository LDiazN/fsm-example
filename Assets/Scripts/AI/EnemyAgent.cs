using System.ComponentModel;
using AI;
using UnityEngine;

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
    }

    #region AI State

    // --------------------------------------------------
    private IState[] states = new IState[3];
    private State _currentState = State.Patrol;

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

    // --------------------------------------------------

    #endregion

    #region Internal State

    // --------------------------------------------------
    private PlayerController _playerController;
    // --------------------------------------------------

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        InitState();
        _playerController = FindObjectOfType<PlayerController>();
    }

    // Update is called once per frame
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
        bool hitPlayer = Physics.Raycast(
            transform.position, 
            toPlayer, 
            out RaycastHit hit, 
            lookDistance
        );
        
        return hitPlayer;
    }

    private bool CanHearPlayer()
    {
        return true;
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