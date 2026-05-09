/// <summary>
/// Defines the lifecycle methods for a state in a finite state machine.
/// </summary>
public interface IState
{
    void OnEnter();
    void Do();
    void FixedDo();
    void OnExit();
}
