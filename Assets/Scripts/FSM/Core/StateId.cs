/// <summary>
/// Represents the unique identifier for a finite state machine state.
/// </summary>
public enum StateId
{
    None = 0,
    Idle = 1,
    Patrol = 2,
    Chase = 3,
    Attack = 4,
    Dead = 5,
    Reposition = 6,
    Investigate = 7,
    ReturnHome = 8
}
