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

    private AI.IState[] states = new AI.IState[3];
    private State _currentState = State.Patrol;
    
    private Perceptions _perceptions;
    public Perceptions GetPerceptions() => _perceptions;
    
    
    // Start is called before the first frame update
    void Start()
    {
        InitState();
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
        
    }

    private void InitState()
    {
        _currentState = State.Patrol;
        states[(uint)State.Patrol] = new Patrol();
        states[(uint)State.Alert] = new Alert();
        states[(uint)State.Chase] = new Chase();
    }
}
