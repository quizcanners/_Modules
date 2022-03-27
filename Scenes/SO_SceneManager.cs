using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_GameController.PROJECT_NAME + "/Managers/" + FILE_NAME)]
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
        public class EnumeratedScene : IPEGI_ListInspect, IGotReadOnlyName, Singleton.ILoadingProgressForInspector, INeedAttention
        {
            public Game.Enums.Scene Type;
            public SerializableSceneReference SceneReference;
            public AsyncOperation LoadOperation;

            private readonly Gate.Frame _onLoadedInitializationOneFrameDelay = new();
            private int _framesSinceLoaded;

            public string ScenePath => SceneReference == null ? "" : SceneReference.ScenePath;

            public bool IsLoadedOrLoading
            {
                get
                {
                    var l = (LoadOperation != null && !LoadOperation.isDone) || IsLoaded;

                    if (IsLoaded && _onLoadedInitializationOneFrameDelay.TryEnter())
                        _framesSinceLoaded++;

                    return l;
                }
                set
                {
                    if (value)
                        Load(LoadSceneMode.Additive);
                    else
                        Unload();
                }
            }

            public bool IsLoadedAndInitialized 
            {
                get 
                {
                    var scene = SceneManager.GetSceneByPath(SceneReference.ScenePath);
                    if (!scene.IsValid())
                        return false;

                    if (IsLoaded && _onLoadedInitializationOneFrameDelay.TryEnter())
                        _framesSinceLoaded++;

                    return _framesSinceLoaded >= 5;
                }
            }

            public bool IsLoaded
            {
                get
                {
                    if (SceneReference == null)
                        return false;

                    var sc = SceneManager.GetSceneByPath(SceneReference.ScenePath);
                    if (!sc.IsValid())
                        return false;

                    return sc.isLoaded;
                }
            }
               
            public void Load(LoadSceneMode mode)
            {
                if (LoadOperation == null || LoadOperation.isDone)
                {
                    if (!IsLoaded)
                    {
                        LoadOperation = SceneManager.LoadSceneAsync(ScenePath, mode);
                        _framesSinceLoaded = 0;
                    }
                }
            }

            private void Unload() 
            {
                if (IsLoaded)
                {
                    SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(ScenePath));
                    LoadOperation = null;
                    _framesSinceLoaded = 0;
                }
            }

            #region Inspector

            public bool IsLoading(ref string state, ref float progress01)
            {
                if (LoadOperation != null && !LoadOperation.isDone) 
                {
                    progress01 = LoadOperation.progress;
                    state = ScenePath;
                    return true;
                }

                return false;
            }

            public void InspectInList(ref int edited, int ind)
            {
                pegi.Edit_Enum(ref Type, width: 120);

                if (IsLoaded)
                {
                    var scene = SceneManager.GetSceneByPath(ScenePath);

                    if (scene.isSubScene == false)
                        "MAIN".PegiLabel(60).Write();
                    else "Unload".PegiLabel().Click(() =>
                        {
                            IsLoadedOrLoading = false;
                            return;
                        });

                    SceneManager.GetSceneByPath(ScenePath).name.PegiLabel().Write();

                }
                else
                {

                    if (LoadOperation != null && LoadOperation.isDone == false)
                    {
                        "Loading {0}... {1}%".F(ScenePath, Mathf.FloorToInt(LoadOperation.progress * 100)).PegiLabel().Write();
                    }
                    else
                    {
                        SceneReference.Nested_Inspect(fromNewLine: false);

                        if (Application.isPlaying)
                        {
                            Icon.Add.Click(()=> IsLoadedOrLoading = true);

                            Icon.Load.Click(()=> Load(LoadSceneMode.Single));
                        }
#if UNITY_EDITOR
                        else if ("Switch".PegiLabel(toolTip: "Save scene before switching to another. Sure you want to change?").ClickConfirm(
                            confirmationTag: "SwSc" + ScenePath))
                                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
#endif

                    }
                }
#if UNITY_EDITOR

                if (ScenePath.IsNullOrEmpty() == false)
                {
                    bool match = false;
                    var allScenes = EditorBuildSettings.scenes;
                    foreach (var sc in allScenes)
                    {
                        if (sc.path.Equals(ScenePath))
                        {
                            match = true;

                            var enbl = sc.enabled;

                            if (pegi.ToggleIcon(ref enbl))
                            {
                                sc.enabled = enbl;
                                EditorBuildSettings.scenes = allScenes;
                            }

                            break;
                        }
                    }

                    if (!match)
                        "Add To Build".PegiLabel().Click(() =>
                        {
                            var lst = new List<EditorBuildSettingsScene>(allScenes)
                            {
                                new EditorBuildSettingsScene(ScenePath, enabled: true)
                            };
                            EditorBuildSettings.scenes = lst.ToArray();
                        });
                }
#endif
            }

            public string GetReadOnlyName() => "{0}: {1}".F(Type.ToString(), ScenePath);

            public string NeedAttention()
            {
                if (SceneReference == null)
                    return "Scene Reference is null";

                return SceneReference.NeedAttention();
            }


            #endregion
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
