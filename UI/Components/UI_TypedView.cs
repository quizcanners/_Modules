using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public abstract class UI_TypedView : MonoBehaviour
    {

        public override string ToString() => MyView.ToString().SimplifyTypeName();

        public abstract Game.Enums.View MyView { get; }
    }
}