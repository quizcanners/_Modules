using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_GameController.PROJECT_NAME + "/" + FILE_NAME)]
    public class SO_SceneManager : ScriptableObject, IPEGI, INeedAttention, Singleton.ILoadingProgressForInspector
    {
        public const string FILE_NAME = "Scenes Manager";

        public List<EnumeratedScene> Scenes;

        public bool IsFullyLoadedAndInitialized(Game.Enums.Scene scene) => TryGet(scene, out var sceneData) && sceneData.IsLoadedAndInitialized;

        public bool this[Game.Enums.Scene scene] 
        {
            get => TryGet(scene, out var match) && match.IsLoadedOrLoading;
            set 
            {
                if (TryGet(scene, out var match))
                    match.IsLoadedOrLoading = value;
            }
        }
   
        public void LoadAndUnloadOthers(List<Game.Enums.Scene> scenes)
        {
            foreach (var s in Scenes)
            {
                s.IsLoadedOrLoading = scenes.Contains(s.Type);
            }
        }


        private bool TryGet(Game.Enums.Scene scene, out EnumeratedScene result)
        {
            foreach (var s in Scenes)
            {
                if (s.Type == scene)
                {
                    result = s;
                    return true;
                }
            }
            result = null;
            return false;
        }

        public string NeedAttention() => pegi.NeedsAttention(Scenes);

        [Serializable]
        public class EnumeratedScene : Qc_SceneInspectable
        {
            public Game.Enums.Scene Type;

            public override void InspectInList(ref int edited, int ind)
            {
                pegi.Edit_Enum(ref Type, width: 120);

                base.InspectInList(ref edited, ind);
            }

            public override string ToString() => "{0}: {1}".F(Type.ToString(), base.ToString());
        }

        #region Inspector
        public void Inspect()
        {
            "Scenes".PegiLabel().Edit_List(Scenes).Nl();
        }

        public bool IsLoading(ref string state, ref float progress01)
        {
            foreach (var s in Scenes)
                if (s.IsLoading(ref state, ref progress01))
                    return true;

            return false;

        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(SO_SceneManager))] internal class SceneManagerScriptableObjectDrawer : PEGI_Inspector_Override { }
}
