using QuizCanners.Inspect;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_GameController.PROJECT_NAME + "/" + FILE_NAME)]
    public class SO_UiViewEnumerated : EnumeratedAssetReferences<Game.Enums.View, GameObject>
    {
        public const string FILE_NAME = "Enumerated Views";
    }


    [PEGI_Inspector_Override(typeof(SO_UiViewEnumerated))] internal class UiViewEnumeratedScriptableObjectDrawer : PEGI_Inspector_Override { }
}
