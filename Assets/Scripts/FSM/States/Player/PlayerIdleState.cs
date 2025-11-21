using Assets.Scripts.FSM.Actors.Player;
using UnityEngine;

namespace Assets.Scripts.FSM.States
{
    public class PlayerIdleState : State
    {
        [SerializeField] private State _moveState;

        private PlayerCore _core;

        public override void OnEnter()
        {
            base.OnEnter();
            _core = (PlayerCore)core;
            _core.StopMovement();
        }

        public override void Do()
        {
            var input = _core.ReadMoveInput();
            if (input != Vector2.zero)
            {
                Set(_moveState, forceReset: true);
                return;
            }
            _core.ApplyMovement(Vector2.zero);
        }

        public override void OnExit() => _core.StopMovement();
    }
}