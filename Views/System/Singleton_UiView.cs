using QuizCanners.Inspect;
using QuizCanners.SpecialEffects;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static QuizCanners.Utils.QcDebug;

namespace QuizCanners.IsItGame.UI
{
    public class Singleton_UiView : IsItGameServiceBase, Singleton.ILoadingProgressForInspector
    {
        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private UI_BlurTransitionSimple _crossFadeTransition;
        [SerializeField] private UI_BlurTransitionSimple _blurTransition;
        [SerializeField] private UI_BlurTransitionSimple _hexTransition;
        [SerializeField] private SO_UiViewEnumerated _viewsInAssets;
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _raycastBlockObject;
        [SerializeField] private List<CachedView> _cachedViews = new();
        [Serializable] public struct CachedView : IPEGI_ListInspect, IGotReadOnlyName
        {
            public Game.Enums.View ViewEnum;
            public GameObject Instance;

            public string GetReadOnlyName() => ViewEnum.ToString();

            public void InspectInList(ref int edited, int index)
            {
                pegi.EditEnum(ref ViewEnum, width: 90);
                pegi.Edit(ref Instance, pegi.UnityObjectSource.Scene); 
            }
        }

        private GameObject _currentViewInstance;
        private InstanceSource _currentViewIsAddressable;
        private Game.Enums.View _targetView = Game.Enums.View.None;
        private Game.Enums.View _currentView = Game.Enums.View.None;
        private ScreenChangeState _screenChangeState = ScreenChangeState.Standby;
        private UiTransitionType _currentTransitionType;
        private UiTransitionType _targetTransitionType;
        private UiRaycastBlock _raycastBlock = UiRaycastBlock.Undecided;
        private UiRaycastBlock RaycastBlock 
        { 
            get => _raycastBlock; 
            set 
            {
                _raycastBlock = value;
                _raycastBlockObject.gameObject.SetActive(_raycastBlock == UiRaycastBlock.On);
            } 
        }

        private enum InstanceSource {Unknown, Addressable, Resources, Cached }

        private readonly List<Game.Enums.View> _viewsStack = new();
        private LogicWrappers.Request _updateBackgroundRequested = new();

        private UI_BlurTransitionSimple TransitionGraphic => _currentTransitionType switch
                {
                    UiTransitionType.Hexagonal => _hexTransition,
                    UiTransitionType.CrossFade => _crossFadeTransition,
                    UiTransitionType.ZoomOut => _blurTransition,
                    _ => _blurTransition,
                };
    
        public void Show(Game.Enums.View view, bool clearStack, UiTransitionType transition = UiTransitionType.CrossFade, bool updateBackground = true) 
        {
            if (updateBackground)
                _updateBackgroundRequested.CreateRequest();

            if (clearStack)
                _viewsStack.Clear();
            else
            {
                var ind = _viewsStack.IndexOf(view);
                if (ind != -1)
                    _viewsStack.RemoveRange(ind, _viewsStack.Count - ind);
                else if (_targetView != Game.Enums.View.None)
                    _viewsStack.Add(_targetView);
            }

            _targetView = view;
            _targetTransitionType = transition;
        }

        public void Hide(Game.Enums.View view, UiTransitionType transition = UiTransitionType.CrossFade)
        {
            if (_targetView == view) 
            {
                _targetTransitionType = transition;
                _targetView = _viewsStack.TryTake(_viewsStack.Count-1, defaultValue: Game.Enums.View.None); 
            } else 
            {
                if (_viewsStack.Contains(view))
                    _viewsStack.Remove(view);
            }
        }

        public void HideCurrent(UiTransitionType transition = UiTransitionType.CrossFade) 
        {
            Hide(_currentView, transition);
        }

        public void ShowError(string text) 
        {
            _targetView = Game.Enums.View.ErrorSorry;
            _screenChangeState = ScreenChangeState.ReadyToChangeView;
            Debug.LogError(text);
        }

        public bool IsLoading(ref string state, ref float progress01)
        {
            if (_screenChangeState == ScreenChangeState.LoadingNextView) 
            {
                state = "LoadingNextView";
                progress01 = handle.PercentComplete;
                return true;
            }

            return false;
        }

        AsyncOperationHandle<GameObject> handle;
        private IDisposable _timer; 

        private void LateUpdate()
        {
            switch (_screenChangeState)
            {
                case ScreenChangeState.Standby:

                    CheckStateMachine();

                    if (_targetView != _currentView)
                    {
                        _screenChangeState = ScreenChangeState.RequestedScreenShot;
                        _currentTransitionType = _targetTransitionType;

                        Singleton_ScreenBlur.ProcessCommand processCommand = Singleton_ScreenBlur.ProcessCommand.Nothing;

                        processCommand = _currentTransitionType switch
                        {
                            UiTransitionType.WipeAway => Singleton_ScreenBlur.ProcessCommand.WashAway,
                            UiTransitionType.ZoomOut => Singleton_ScreenBlur.ProcessCommand.ZoomOut,
                            _ => Singleton_ScreenBlur.ProcessCommand.Blur,
                        };

                        TransitionGraphic.SetObscure(
                            onObscured: () => _screenChangeState = ScreenChangeState.ReadyToChangeView,
                            processCommand,
                            updateBackground: _updateBackgroundRequested.TryUseRequest());
                    }
                    break;
                case ScreenChangeState.RequestedScreenShot: break;
                case ScreenChangeState.ReadyToChangeView:

                    _screenChangeState = ScreenChangeState.LoadingNextView;
                    _currentView = _targetView;

                    DestroyInstance();

                    if (_currentView == Game.Enums.View.None)
                    {
                        _screenChangeState = ScreenChangeState.ViewIsSetUp;
                        break;
                    }

                    GameObject cached = null;
                    foreach (var c in _cachedViews)
                        if (c.ViewEnum == _currentView && c.Instance)
                            cached = c.Instance;

                    if (cached)
                    {
                        FinalizeSetup(cached, InstanceSource.Cached);
                    }
                    else
                    {
                        var reff = _viewsInAssets.GetReference(_currentView);

                        if (reff == null)
                        {
                            ShowError("Reference {0} not found".F(_currentView));
                            _screenChangeState = ScreenChangeState.ViewIsSetUp;
                            return;
                        }

                        _timer = timerGlobal.StartSaveMaxTimer(key: nameof(Singleton_UiView), details: _currentView.ToString()); //GetCollection("{0}".F(nameof(UiViewService))).StartTimer(_currentView.ToString());

                        if (reff.IsReferenceVaid)
                        {
                            handle = reff.Reference.InstantiateAsync(_root);
                            handle.Completed += result =>
                            {
                                if (result.Status == AsyncOperationStatus.Succeeded)
                                {
                                    FinalizeSetup(result.Result, InstanceSource.Addressable);
                                }
                                else
                                {
                                    ShowError("Couldn't load the {0} view".F(_currentView));
                                }
                            };
                        }
                        else
                        {
                            if (!reff.DirectReference)
                            {
                                ShowError("No References for {0} found".F(_currentView));
                                return;
                            }

                            FinalizeSetup(Instantiate(reff.DirectReference, _root) as GameObject, InstanceSource.Resources);
                        }
                    }

                    break;

                case ScreenChangeState.LoadingNextView: break;
                case ScreenChangeState.ViewIsSetUp:

                    UiObscureScreen obs = UiObscureScreen.Off;
                    if (Game.State.TryChangeFallback(ref obs, fallbackValue: UiObscureScreen.Off)) 
                    {
                        if (obs == UiObscureScreen.On)
                            return;
                    }

                    _timer.Dispose();

                    if (_loadingScreen)
                        _loadingScreen.SetActive(false);

                    TransitionGraphic.Reveal();
                    _screenChangeState = ScreenChangeState.Standby;

                    break;
            }

            void FinalizeSetup(GameObject instance, InstanceSource source)
            {
                _currentViewIsAddressable = source;
                _screenChangeState = ScreenChangeState.ViewIsSetUp;
                _currentViewInstance = instance;
                _currentViewInstance.SetActive(true);
            }
        }

        #region Inspector
        public override string InspectedCategory => Utils.Singleton.Categories.ROOT;

        private readonly pegi.EnterExitContext contenxt = new();

        private Game.Enums.View _debugType = Game.Enums.View.None;
        public override void Inspect()
        {
            pegi.Nl();
            using (contenxt.StartContext())
            {
                if (contenxt.IsAnyEntered == false)
                {
                    if (Application.isPlaying)
                    {
                        "Screen Change State: {0}".F(_screenChangeState).PegiLabel().Nl();

                        "Transition Type".PegiLabel(90).EditEnum(ref _targetTransitionType).Nl();

                        "Target view".PegiLabel(90).EditEnum(ref _debugType);

                        Icon.Add.Click(() => Show(_debugType, clearStack: false, _targetTransitionType));

                        Icon.Play.Click(() => Show(_debugType, clearStack: true, _targetTransitionType));

                        pegi.Nl();

                        if (_targetView != Game.Enums.View.None)
                        {
                            Icon.Clear.Click(() => Hide(_targetView));
                            _targetView.ToString().PegiLabel().Write();
                            pegi.Nl();
                        }

                        for (int i = _viewsStack.Count - 1; i >= 0; i--)
                        {
                            var v = _viewsStack[i];
                            ; if (Icon.Close.Click())
                                Hide(v);

                            v.ToString().PegiLabel().Write();

                            pegi.Nl();
                        }

                        if (_currentViewInstance)
                            "Destroy {0}".F(_currentViewInstance.name).PegiLabel().Click(() => Addressables.Release(_currentViewInstance)).Nl();
                    }
                }

                "Enumerated Views".PegiLabel().Edit_Enter_Inspect(ref _viewsInAssets).Nl();
                
                pegi.Nl();
                
                "Cached".PegiLabel().Enter_List(_cachedViews).Nl();
            }
        }

        public override void InspectInList(ref int edited, int ind)
        {
            if ("View".PegiLabel(40).EditEnum(ref _debugType))
                Show(_debugType, clearStack: false);

            if (Icon.Enter.Click())
                edited = ind;
        }

        public void InspectCurrentView() 
        {
            if (!_currentViewInstance)
                "No views".PegiLabel().Nl();
            else
            {
                _currentViewInstance.name.PegiLabel(style: pegi.Styles.ListLabel).Nl();
                pegi.Try_Nested_Inspect(_currentViewInstance.GetComponent<IPEGI>());
            }
        }

        #endregion

        protected void Awake()
        {
            if (_loadingScreen)
                _loadingScreen.SetActive(true);

            foreach (var c in _cachedViews)
                if (c.Instance)
                    c.Instance.SetActive(false);
        }

        private void DestroyInstance() 
        {
            if (_currentViewInstance)
            {
                switch (_currentViewIsAddressable) 
                {
                    case InstanceSource.Addressable: Addressables.Release(_currentViewInstance); break;
                    case InstanceSource.Resources: _currentViewInstance.DestroyWhatever(); break;
                    case InstanceSource.Cached: _currentViewInstance.SetActive(false); break;
                }

                _currentViewInstance = null;
            }
        }

        private void CheckStateMachine()
        {
            if (TryEnterIfStateChanged())
            {
                Game.Enums.View newView = _targetView;
                if (Game.State.TryChangeFallback(ref newView, fallbackValue: Game.Enums.View.None))
                {
                    if (newView != Game.Enums.View.None && !_viewsStack.Contains(newView))
                    {
                        Show(newView, clearStack: true, transition: UiTransitionType.Blur);
                    }
                }

                if (Game.State.TryChangeFallback(ref _raycastBlock, fallbackValue: UiRaycastBlock.Off))
                    RaycastBlock = _raycastBlock;
            }
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode()
        {
            DestroyInstance();

            _targetView = Game.Enums.View.None;
            _currentView = Game.Enums.View.None;
            _screenChangeState = ScreenChangeState.Standby;
        }

        protected enum ScreenChangeState { Standby, RequestedScreenShot, ReadyToChangeView, LoadingNextView, ViewIsSetUp }
    }

    [PEGI_Inspector_Override(typeof(Singleton_UiView))] internal class UiViewServiceDrawer : PEGI_Inspector_Override { }

    public enum UiTransitionType { Blur, CrossFade, Hexagonal, WipeAway, ZoomOut }

    public enum UiRaycastBlock { Undecided, Off, On }

    public enum UiObscureScreen { Off, On }

    public static class UiViewsExtensions 
    {
        public static void Show(this Game.Enums.View view, bool clearStack, UiTransitionType transition = UiTransitionType.CrossFade, bool updateBackground = true)
            => Singleton.Try<Singleton_UiView>(s=> s.Show(view, clearStack, transition, updateBackground));

        public static void Hide(this Game.Enums.View view, UiTransitionType transition = UiTransitionType.CrossFade)
            => Singleton.Try<Singleton_UiView>(s => s.Hide(view, transition));
    }
}
