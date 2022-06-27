using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

namespace QuizCanners.SpecialEffects
{

    [ExecuteAlways]
    public class Pool_CurrencyAnimationController : Singleton.BehaniourBase
    {
        private readonly Dictionary<SO_CurrencyAnimationPrototype, CurrencyHub> _currencies = new Dictionary<SO_CurrencyAnimationPrototype, CurrencyHub>();
        [SerializeField] private SO_CurrencyAnimationSettings _settings;
        [SerializeField] private C_CurrencyAnimationElement _prefab;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private AudioSource _source;
        [SerializeField] private Canvas _canvas;

        private static Pool_CurrencyAnimationController Instance => Singleton.Get<Pool_CurrencyAnimationController>();


        private readonly List<C_CurrencyAnimationElement> _instances = new List<C_CurrencyAnimationElement>();
        private readonly List<C_CurrencyAnimationElement> _pool = new List<C_CurrencyAnimationElement>();
        private readonly LogicWrappers.TimeFixedSegmenter _spawnings = new LogicWrappers.TimeFixedSegmenter(unscaledTime: true);

        private readonly Gate.UnityTimeUnScaled _spawnSoundDelay = new Gate.UnityTimeUnScaled(Gate.InitialValue.StartArmed);

        private int carousel = 0;

        public double GetAnimatedValue(SO_CurrencyAnimationPrototype currency)
        {
            var cur = _currencies.GetOrCreate(currency);

            if (cur.IsAnimating)
                return cur.Value.GetWithoutUpdating();
            else
                return cur.Value.UpdateAndGet();

        }

        public double GetAnimatedValue(SO_CurrencyAnimationPrototype currency, double targetValue)
        {
            var cur = _currencies.GetOrCreate(currency);
            cur.Value.TargetValue = targetValue;
            if (cur.IsAnimating)
                return cur.Value.GetWithoutUpdating();
            else
                return cur.Value.UpdateAndGet();
        }

        public void RequestAnimation(SO_CurrencyAnimationPrototype currency, RectTransform origin) 
        {
            _currencies.GetOrCreate(currency).SetRequest(new CurrencyHub.AnimationRequest(origin));
        }

        public void RequestAnimation(SO_CurrencyAnimationPrototype currency, RectTransform origin, double targetValue)
        {
            CurrencyHub cur = _currencies.GetOrCreate(currency);
            cur.Value.TargetValue = targetValue;
            cur.SetRequest(new CurrencyHub.AnimationRequest(origin));
        }

        public void RequestAnimation(SO_CurrencyAnimationPrototype currency, Vector2 origin, Vector2 areaSize)
        {
            _currencies.GetOrCreate(currency).SetRequest(new CurrencyHub.AnimationRequest(origin, areaSize));
        }

        public void RequestAnimation(SO_CurrencyAnimationPrototype currency, Vector2 origin, Vector2 areaSize, double targetValue)
        {
            CurrencyHub cur = _currencies.GetOrCreate(currency);
            cur.Value.TargetValue = targetValue;
            cur.SetRequest(new CurrencyHub.AnimationRequest(origin, areaSize));
        }

        public void RegisterAnimationTarget (SO_CurrencyAnimationPrototype currency, C_CurrencyAnimationConsumer _target) 
        {
            _currencies.GetOrCreate(currency).TargetStack.AddTarget(_target);
        }

        public void RemoveAnimationTarget(SO_CurrencyAnimationPrototype currency, C_CurrencyAnimationConsumer _target)
        {
            _currencies.GetOrCreate(currency).TargetStack.RemoveTarget(_target);
        }

        public double GetTargetValue(SO_CurrencyAnimationPrototype currency) => _currencies.GetOrCreate(currency).Value.TargetValue;

        public void Clear()
        {
            _instances.DestroyAndClear();
            _pool.DestroyAndClear();
        }

        internal void Return(C_CurrencyAnimationElement element) 
        {
            if (_source && _spawnSoundDelay.TryUpdateIfTimePassed(_settings.SOUND_EFFECT_MIN_GAP))
            {
                if (element.Prototype.TryGetRandomConsumeSound(out var clip))
                    _source.PlayOneShot(clip);
            }

            element.Currency.ReturnValue((int)element.ValueToDeliver);
            element.gameObject.SetActive(false);
            _instances.Remove(element);
            _pool.Add(element);
            element.Currency.TargetStack.Wobble();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || _currencies.Count == 0)
                return;

            int segments = _spawnings.GetSegmentsWithouUpdate(segment: _settings.DELAY_BETWEEN_ANIMATIONS);

            if (segments > 0) 
            {
                carousel = (carousel + 1) % _currencies.Count;
                KeyValuePair<SO_CurrencyAnimationPrototype, CurrencyHub> el = _currencies.GetElementAt(carousel);

                var currencyState = el.Value;
                var currencyPrototype = el.Key;

                if (currencyState.TryGetOne(out int value)) 
                {
                    C_CurrencyAnimationElement isnt = Spawn();

                    C_CurrencyAnimationElement Spawn() 
                    {
                        C_CurrencyAnimationElement newInstance;

                        if (_pool.Count > 0)
                        {
                            newInstance = _pool.TryTakeLast();
                        }
                        else
                        {
                            newInstance = Instantiate(_prefab, transform);
                        }

                        _instances.Add(newInstance);
                        return newInstance;
                    }

                    isnt.Restart(currencyState, currencyPrototype, value, _rectTransform);
                    _spawnings.Update(_settings.DELAY_BETWEEN_ANIMATIONS);

                    if (_source && _spawnSoundDelay.TryUpdateIfTimePassed(_settings.SOUND_EFFECT_MIN_GAP))
                    {
                        if (currencyPrototype.TryGetRandomCreateSound(out var clip))
                            _source.PlayOneShot(clip);
                    }

                } else 
                {
                    //TODO: Call This Externally
                    if (currencyState.IsAnimating == false)
                        currencyState.Value.UpdateAndGet();
                }
            }
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            base.OnBeforeOnDisableOrEnterPlayMode(afterEnableCalled);

            Clear();
        }

        #region Inspector

        private readonly pegi.EnterExitContext _context = new();
        private readonly pegi.CollectionInspectorMeta _currenciesMeta = new pegi.CollectionInspectorMeta("Currencies");
        private SO_CurrencyAnimationPrototype _testKey;

        public override void Inspect()
        {
            pegi.Nl();
            using (_context.StartContext())
            {
                "Settings".PegiLabel().Edit_Enter_Inspect(ref _settings).Nl();

                if ("Currency Animations".PegiLabel().IsConditionally_Entered(canEnter: Application.isPlaying, showLabelIfTrue: false).Nl())
                    _currenciesMeta.Edit_Dictionary(_currencies).Nl();

                if (_context.IsAnyEntered == false)
                {
                    if (Application.isPlaying)
                    {
                        if (_pool.Count > 0 || _instances.Count > 0)
                            Icon.Clear.Click();

                        "Pool: {0} / Instances: {1}".F(_pool.Count, _instances.Count).PegiLabel().Nl();

                        "Test Key".PegiLabel().Select(ref _testKey, _currencies.Keys.ToList());

                        if (Icon.Play.Click())
                            RequestAnimation(_testKey, null);

                        pegi.Nl();
                    }


                    "Root".PegiLabel(60).Edit_IfNull(ref _rectTransform, gameObject).Nl();

                    "Audio Source".PegiLabel(90).Edit_IfNull(ref _source, gameObject).Nl();

                    "Canvas".PegiLabel(60).Edit_IfNull(ref _canvas, gameObject).Nl();

                    "Prefab".PegiLabel(50).Edit(ref _prefab).Nl();

                    if (_canvas) 
                    {
                        if ((_canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                            "Canvas Needs to have Texcoord1 Channel enebled".PegiLabel().WriteWarning().Nl();

                        if (_canvas.renderMode != RenderMode.ScreenSpaceCamera)
                        {
                            "Canvas need to use {0}".F(RenderMode.ScreenSpaceCamera).PegiLabel().WriteWarning().Nl();
                            if ("Fix Canvas".PegiLabel().Click())
                                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        }
                    }
                    if (!C_UiCameraForEffectsManagement.Camera)
                        "No UI Camera".PegiLabel().WriteWarning().Nl();

                }
            }
        }

        #endregion

        internal class CurrencyHub : IPEGI, IPEGI_ListInspect
        {
            public AnimatedValue.Double Value = new AnimatedValue.Double();
            public AnimationRequest Request;
            public readonly AnimationTarget TargetStack = new AnimationTarget();
            private int _valueInAnimation;

            public bool IsAnimating => _valueInAnimation > 0;

            private int _elementsToSpawn;

            int CalculateCurrencyToAnimate() => (int)(Value.TargetValue - Value.GetWithoutUpdating()) - _valueInAnimation;

            private static Pool_CurrencyAnimationController Mgmt => Singleton.Get<Pool_CurrencyAnimationController>();

            public bool IsValid => Request != null && Request.IsValid && TargetStack.IsValid;

            internal void SetRequest(AnimationRequest request)
            {
                Request = request;
                int instancesCount = Mgmt._instances.Count;
                int deltaValue = CalculateCurrencyToAnimate();

                if (deltaValue < 1)
                {
                    return;
                }

                float fraction = Value.TargetValue <= 0 ? 1 : Mathf.Clamp01((float)(deltaValue / Value.TargetValue));

                fraction = Mathf.Sqrt(fraction);

                int optimal = (Mgmt._settings.MAX_ELEMENTS - instancesCount) / 2;

                _elementsToSpawn = Math.Clamp(value: 4 + Mathf.RoundToInt(optimal * fraction), min: 1, max: Math.Min(optimal, deltaValue));
            }

            internal void ReturnValue(int value) 
            {
                _valueInAnimation -= value;
                Value.SetCurrentValue(Value.GetWithoutUpdating() + value);
            }

            internal bool TryGetOne(out int value) 
            {
                value = 0;

                if (!IsValid)
                    return false;

                if (_elementsToSpawn < 1)
                    return false;

                int currently = Mgmt._instances.Count;

                if (currently >= Mgmt._settings.MAX_ELEMENTS)
                    return false;

              
                value = CalculateCurrencyToAnimate() / _elementsToSpawn;

                _elementsToSpawn--;

                _valueInAnimation += value;

                Request.Revalidate();
                return true;
            }

            #region Inspector

            public void Inspect()
            {
                "Request Valid: {0}".F(IsValid).PegiLabel().Nl();
                Value.Nested_Inspect().Nl();
                "Elements to spawn: {0}".F(_elementsToSpawn).PegiLabel().Nl();
                "In Animation: {0}".F(_valueInAnimation).PegiLabel().Nl();
                "Target Position: {0}".F(TargetStack.GetTargetPosition().ToString()).PegiLabel().Nl();
            }

            public void InspectInList(ref int edited, int index)
            {
                if (TargetStack.IsValid) 
                {
                    Icon.Done.Draw(toolTip: "Target is Ready");
                }

                Value.ToString().PegiLabel().Write();

                if (Icon.Enter.Click())
                    edited = index;

            }

            #endregion

            public class AnimationRequest
            {
                public RectTransform Origin;
                private Vector2 _lastPosition;
                private Vector2 _rectSize;
                private readonly Gate.UnityTimeUnScaled _unscaledTime = new(initialValue: Gate.InitialValue.InitializeOnCreate);

                public bool IsValid => _unscaledTime.GetDeltaWithoutUpdate() < 0.5f;

                public void Revalidate() => _unscaledTime.Update();

                public Vector2 GetOriginPosition()
                {
                    if (Origin)
                    {
                        if (!Origin.gameObject.activeInHierarchy)
                            Origin = null;
                        else
                        {
                            _lastPosition = RectTransformUtility.WorldToScreenPoint(C_UiCameraForEffectsManagement.Camera, Origin.position);
                        }
                    }

                    return _lastPosition + new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f)) * _rectSize;
                }

                public AnimationRequest(RectTransform origin)
                {
                    Origin = origin;
                    _unscaledTime.Update();

                    if (!Origin)
                    {
                        _lastPosition = new Vector2(Random.value, Random.value) * new Vector2(Screen.width, Screen.height);
                        _rectSize = Vector2.one * 20f;
                    }
                    else
                    {
                        _lastPosition = RectTransformUtility.WorldToScreenPoint(C_UiCameraForEffectsManagement.Camera, Origin.position);
                        _rectSize = Origin.rect.size;
                    }
                }

                public AnimationRequest(Vector2 screenPosition, Vector2 rectSize)
                {
                    _unscaledTime.Update();

                    _lastPosition = screenPosition;
                    _rectSize = rectSize;

                }
            }

            public class AnimationTarget
            {
                private List<C_CurrencyAnimationConsumer> _targetStack = new List<C_CurrencyAnimationConsumer>();
                private Vector2 _lastPosition;
                private Gate.Frame _positionUpdateGate = new Gate.Frame();

                private C_CurrencyAnimationConsumer Target => _targetStack.Count > 0 ? _targetStack.Last() : null;

                public bool IsValid => Target && Target.gameObject.activeInHierarchy;

                public void Wobble()
                {
                    if (Target)
                        Target.Wobble();
                }
                

                public Vector2 GetTargetPosition()
                {
                    if (Target && _positionUpdateGate.TryEnter())
                    {
                        _lastPosition = RectTransformUtility.WorldToScreenPoint(C_UiCameraForEffectsManagement.Camera, Target.rectTransform.position);
                    }

                    return _lastPosition;
                }

                public void AddTarget(C_CurrencyAnimationConsumer target)
                {
                    _targetStack.Add(target);
                    GetTargetPosition();
                }

                public void RemoveTarget(C_CurrencyAnimationConsumer target) 
                {
                    _targetStack.Remove(target);
                }
            }

        }

        private void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
    }

    [PEGI_Inspector_Override(typeof(Pool_CurrencyAnimationController))] internal class Singleton_CurrencyAnimationCanvasDrawer : PEGI_Inspector_Override { }
}