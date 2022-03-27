using UnityEngine;
using QuizCanners.Inspect;


namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_GameController.PROJECT_NAME + "/Managers/Audio/" + FILE_NAME)]

    public class SO_Music_EnumeratedCollection : EnumeratedAssetListsBase<Game.Enums.Music, SO_Music_ClipData>
    {
        public const string FILE_NAME = "Enumerated Music";
    }

    [PEGI_Inspector_Override(typeof(SO_Music_EnumeratedCollection))] internal class CoreLocatorEnumeratedMusicDrawer : PEGI_Inspector_Override { }

}