using UnityEngine;
using Assets.Scripts.FSM.Actors.Player;

namespace Assets.Scripts.FSM.States
{
    public class PlayerMoveState : State
    {
        [Header("Transitions")]
        [SerializeField] private PlayerIdleState _idleState;

        private PlayerCore _core;

        public override void OnEnter()
        {
            base.OnEnter();
            _core = (PlayerCore)core;
        }

        public override void Do()
        {
            var input = _core.ReadMoveInput();
            if (input == Vector2.zero)
            {
                Set(_idleState, forceReset: true);
                return;
            }
            _core.ApplyMovement(input);
        }

        public override void OnExit() => _core.StopMovement();
    }
}