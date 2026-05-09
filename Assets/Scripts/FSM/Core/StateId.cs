/// <summary>
/// Represents the unique identifier for a state in the finite state machine. This is used to request transitions by id from states and the state machine core.
/// </summary>
public enum StateId
{
    None = 0,
    Idle = 1,
    Patrol = 2,
    Chase = 3,
    Attack = 4,
    Dead = 5,

    PlayerIdle = 100,
    PlayerMove = 101
}
