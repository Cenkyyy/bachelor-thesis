public enum ActorStateId
{
    None = 0,
    Idle = 1,
    Patrol = 2,
    Alert = 3,
    Chase = 4,
    Attack = 5,
    Hurt = 6,
    Return = 7,
    Dead = 8,

    PlayerIdle = 100,
    PlayerMove = 101,
    // TODO: Add more player states (e.g. , PlayerAttack, PlayerHurt, PlayerDead)
}
