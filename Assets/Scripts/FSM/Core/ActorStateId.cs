public enum ActorStateId
{
    None = 0,
    Idle = 1,
    Patrol = 2,
    Chase = 3,
    Attack = 4,
    Dead = 5,

    PlayerIdle = 100,
    PlayerMove = 101,
    // TODO: Add more player states (e.g. , PlayerAttack, PlayerHurt, PlayerDead)
}
