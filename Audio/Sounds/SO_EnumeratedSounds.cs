using UnityEngine;
using QuizCanners.Inspect;

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Quiz Canners/" + Singleton_GameController.PROJECT_NAME + "/Managers/Audio/" + FILE_NAME)]
    public class SO_EnumeratedSounds : EnumeratedAssetListsBase<IigEnum_SoundEffects, AudioClip>
    {
        public const string FILE_NAME = "Enumerated Sounds";


    }

    [PEGI_Inspector_Override(typeof(SO_EnumeratedSounds))] internal class CoreLocatorEnumeratedSoundsDrawer : PEGI_Inspector_Override { }

}
