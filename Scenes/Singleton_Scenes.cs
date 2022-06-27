using QuizCanners.Inspect;
using QuizCanners.IsItGame.StateMachine;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    [ExecuteAlways]
    public class Singleton_Scenes : IsItGameServiceBase, INeedAttention, Singleton.ILoadingProgressForInspector
    {
        [SerializeField] private SO_SceneManager _scenes;

        public bool IsLoadedAndInitialized(Game.Enums.Scene scene) => _scenes.IsFullyLoadedAndInitialized(scene); //.TryGet(scene, out var match) && match.IsLoadedFully;
        public bool IsLoadedOrLoading(Game.Enums.Scene scene) => _scenes[scene];
        public bool SetIsLoading(Game.Enums.Scene scene, bool value) => _scenes[scene] = value;

        public void Update()
        {
            if (TryEnterIfStateChanged()) 
            {
                _scenes.LoadAndUnloadOthers(GameState.Machine.GetAllAdditive<Game.Enums.Scene>());
            }
        }

        public bool IsLoading(ref string state, ref float progress01)
        {
            if (_scenes)
                return _scenes.IsLoading(ref state, ref progress01);

            return false;
        }

        #region Inspector

        public override string InspectedCategory => Utils.Singleton.Categories.SCENE_MGMT;

        private int _inspectedStuff = -1;

        public override void Inspect()
        {
            pegi.Nl();
            "Scenes".PegiLabel().Edit_Enter_Inspect(ref _scenes, ref _inspectedStuff, 0).Nl();
        }

        private Game.Enums.Scene _debugScene = Game.Enums.Scene.None;

        public override void InspectInList(ref int edited, int ind)
        {
            "Scene".PegiLabel(50).Edit_Enum(ref _debugScene);

            if (_debugScene != Game.Enums.Scene.None) 
            {
                if (IsLoadedAndInitialized(_debugScene))
                {
                    Icon.Done.Draw();
                    if (Icon.Clear.Click())
                        SetIsLoading(_debugScene, false);
                }
                else if (IsLoadedOrLoading(_debugScene))
                    Icon.Wait.Draw();
                else if (Icon.Play.Click())
                    SetIsLoading(_debugScene, true);
            }

            if (Icon.Enter.Click())
                edited = ind;

            pegi.ClickHighlight(this);

        }

        public override string NeedAttention()
        {
            if (!_scenes)
                return "Scenes not assigned";

            var na = _scenes.NeedAttention();

            if (!na.IsNullOrEmpty())
                return na;

            return base.NeedAttention();
            
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_Scenes))] internal class ScenesServiceDrawer : PEGI_Inspector_Override { }
}