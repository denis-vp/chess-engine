using chess_engine.Engine;
using chess_engine.UI;

namespace chess_engine.Game
{
    public class ChessPlayer
    {
        public readonly GameController.PlayerType PlayerType;
        public readonly EnginePlayer? Engine;
        public readonly HumanPlayer? Human;

        public ChessPlayer(object instance, GameController.PlayerType type)
        {
            this.PlayerType = type;
            Engine = instance as EnginePlayer;
            Human = instance as HumanPlayer;
        }

        public bool IsHuman => Human != null;
        public bool IsEngine => Engine != null;

        public void Update()
        {
            if (Human != null)
            {
                Human.Update();
            }
        }

        public void GetEngineMove()
        {
            if (Engine != null)
            {
                Engine.GetMove();
            }
        }

        public void SubscribeToMoveChosenEventIfHuman(Action<Move> action)
        {
            if (Human != null)
            {
                Human.MoveChosen += action;
            }
        }
    }
}
