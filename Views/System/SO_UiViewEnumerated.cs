using QuizCanners.Inspect;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Quiz Canners/" + Singleton_GameController.PROJECT_NAME + "/Managers/" + FILE_NAME)]
    public class SO_UiViewEnumerated : EnumeratedAssetReferences<Game.Enums.View, GameObject>
    {
        public const string FILE_NAME = "Enumerated Views";
    }


    [PEGI_Inspector_Override(typeof(SO_UiViewEnumerated))] internal class UiViewEnumeratedScriptableObjectDrawer : PEGI_Inspector_Override { }
}
