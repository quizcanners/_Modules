using UnityEngine;
using QuizCanners.Inspect;

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_GameController.PROJECT_NAME + "/Audio/" + FILE_NAME)]
    public class SO_EnumeratedSounds : EnumeratedAssetListsBase<Game.Enums.UiSoundEffects, AudioClip>
    {
        public const string FILE_NAME = "Enumerated Sound Effects";


    }

    [PEGI_Inspector_Override(typeof(SO_EnumeratedSounds))] internal class CoreLocatorEnumeratedSoundsDrawer : PEGI_Inspector_Override { }

}
