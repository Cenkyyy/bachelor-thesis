using UnityEngine;
using Assets.Scripts.FSM.Actors.Player;

namespace Assets.Scripts.FSM.States
{
    public class PlayerMoveState : State
    {
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
                Set(ActorStateId.PlayerIdle, forceReset: true);
                return;
            }
            _core.ApplyMovement(input);
        }

        public override void OnExit() => _core.StopMovement();
    }
}