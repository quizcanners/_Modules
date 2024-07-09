using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Modules.GameStateMachine
{
    public static class StateMachine  //: IPEGI
    {
        public static bool TryChange(Gate.Integer gate) => 
            Application.isPlaying
            && _stateStack.Count>0
            && gate.TryChange(Version);

        public static int Version { get; private set; }

        private static readonly List<GameState_Base> _stateStack = new();

        public static void SetDirty() => Version++;

        public static void ModifyCollectionByStack<V>(HashSet<V> vals)
        {
            if (TryGetFallback(out V newValue))
                vals.Add(newValue);

            for (int i = 0; i < _stateStack.Count; i++)
            {
                var state = _stateStack[i];

                if (state is IDataAdditive<V> addSt)
                {
                    try
                    {
                        var result = addSt.Get();
                        vals.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (state is IDataSubtractive<V> subSt)
                {
                    try
                    {
                        var result = subSt.Get();
                        vals.Remove(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

#if UNITY_EDITOR
                    if (state is IDataSubtractive_List<V> subLst)
                        Debug.LogError("State {0} implements both {1} and {2}. List will not be processed".F(state, nameof(IDataSubtractive<V>), nameof(IDataSubtractive_List<V>)));
#endif

                } else if (state is IDataSubtractive_List<V> subLst) 
                {
                    try
                    {
                        var result = subLst.Get();
                        foreach (var e in result)
                        {
                            vals.Remove(e);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public static V Get<V>(V defaultValue)
        {
            if (!TryGetFallback(out V newValue))
            {
                return defaultValue;
            }

            return newValue;
        }

        public static bool TryChangeFallback<V>(ref V value) => TryChangeFallback(ref value, value);

        public static bool TryChangeFallback<V>(ref V value, V defaultValue)
        {
            if (!TryGetFallback(out V newValue))
            {
                value = defaultValue;
                return false;
            }

            var different = !newValue.Equals(value);

            value = newValue;

            return different;
        }

        public static bool IsCurrent(GameState_Base state) => state == _stateStack.TryGetLast();

        public static bool IsCurrent(Type state)
        {
            var current = _stateStack.TryGetLast();
            if (current == null)
            {
                return false;
            }
            return state == current.GetType(); 
        }

        public static bool IsInStack(Type type)
        {
            foreach (var s in _stateStack)
            {
                if (s.GetType() == type)
                    return true;
            }

            return false;
        }

        public static void Enter<T>() where T : GameState_Base, new() => Enter(new T());

        public static void Enter(GameState_Base state)
        {
            SetDirty();

            var type = state.GetType();
            if (_stateStack.Any(st => st.GetType() == type))
            {
                //  Debug.LogError("Type {0} is already in the list. Returning.".F(type));
                ReturnToState(type);
                return;
            }

            _stateStack.Add(state);
            Current(cur => cur.OnIsCurrentChange());
            Previous(prev => prev.OnIsCurrentChange());
            Current(state => state.OnEnter());
        }

        public static void Exit(Type type)
        {
            var index = _stateStack.FindLastIndex(state => state.GetType() == type);

            if (index == -1)
            {
                Debug.LogWarning("State {0} not found".F(type));
                return;
            }

            Exit(_stateStack[index]);
        }

        public static void Exit(GameState_Base closedState)
        {
            bool isLast = _stateStack.IndexOf(closedState) == _stateStack.Count - 1;

            if (!isLast)
            {
                ReturnToState(closedState.GetType());
            }
            ExitLast();
        }

        public static void ReturnToState(Type type)
        {
            while (_stateStack.Count > 0 && _stateStack.Last().GetType() != type)
                ExitLast();

            if (_stateStack.Count == 0)
                Debug.LogError("State {0} was never found".F(type.ToPegiStringType()));

            SetDirty();
        }

        public static bool TryGetFallback<V>(out V result)
        {
            for (int i = _stateStack.Count - 1; i >= 0; i--)
            {
                var state = _stateStack[i];

                if (state is IDataFallback<V> st)
                {
                    try
                    {
                        result = st.Get();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    continue;
                }

                if (state is IDataFallback_Conditional<V> st_c) 
                {
                    try
                    {
                        if (st_c.TryGet(out result))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (state is IDataFallback_Break<V>) 
                {
                    result = default;
                    return false;
                }
            }

            result = default;

            return false;
        }

        private static void ExitLast()
        {
            var closedState = _stateStack.Last();
            DoInternal(state => state.OnExit(), closedState);
            _stateStack.Remove(closedState);
            DoInternal(state => state.OnIsCurrentChange(), closedState);
            Current(cur => cur.OnIsCurrentChange());

            SetDirty();
        }

        private static void Current(Action<GameState_Base> action)
        {
            var last = _stateStack.TryGetLast();
            if (last != null)
            {
                DoInternal(action, last);
            }
        }

        private static void Previous(Action<GameState_Base> action)
        {
            var previous = _stateStack.TryGet(_stateStack.Count - 2);
            if (previous != null)
            {
                DoInternal(action, previous);
            }
        }

        private static void DoInternal(Action<GameState_Base> action, GameState_Base state)
        {
            try
            {
                action?.Invoke(state);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            SetDirty();
        }

        public static void ManagedOnDisable()
        {
            while (_stateStack.Count > 0)
            {
                Exit(_stateStack[^1].GetType());
            }
        }

        private static readonly Gate.Frame _updateGate = new();
        private static readonly Gate.Frame _lateUpdateGate = new();

        public static void ManagedUpdate()
        {
            if (!_updateGate.TryEnter())
                return;

            ProcessFallbacks(state => state.Update());

            if (_stateStack.Count > 0)
            {
                _stateStack.Last().UpdateIfCurrent();
            }
        }

        public static void ManagedLateUpdate()
        {
            if (!_lateUpdateGate.TryEnter())
                return;

            ProcessFallbacks(state => state.LateUpdate());
        }

        private static void ProcessFallbacks(Action<GameState_Base> stackOperator)
        {
            int preVersion = Version;

            HashSet<Type> processedStates = new();

            for (int i = _stateStack.Count - 1; i >= 0; i--)
            {
                var st = _stateStack[i];

                processedStates.Add(st.GetType());

                try
                {
                    stackOperator(st);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return;
                }

                if (preVersion != Version)
                    break;
            }

            if (preVersion == Version)
                return;

     
            int maxRecursions = 16;

            while (preVersion != Version && maxRecursions > 0) 
            {
                preVersion = Version;
                maxRecursions--;

                for (int i = _stateStack.Count - 1; i >= 0; i--)
                {
                    var st = _stateStack[i];
                    var type = st.GetType();

                    if (processedStates.Contains(type))
                        continue;

                    processedStates.Add(type);

                    try
                    {
                        stackOperator(st);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        return;
                    }

                    if (preVersion != Version)
                        break;
                }
            }

            if (maxRecursions <= 0) 
            {
                Debug.LogError("State maching ran too many updates");
            }

        }

        #region Inspector

        private static bool _showTest;
        private static int _inspectedState = -1;

      //  private static Game.Enums.GameState _debugState = Game.Enums.GameState.Bootstrap;
        public static void Inspect()
        {

            if ("Version ++ ({0})".F(Version).PegiLabel().Click().Nl())
                SetDirty();

            if (_inspectedState >= _stateStack.Count)
                _inspectedState = -1;

            if (_inspectedState > -1)
            {
                if (Icon.Back.Click() | _stateStack[_inspectedState].GetNameForInspector().PegiLabel().ClickLabel())
                    _inspectedState = -1;
                else
                    _stateStack[_inspectedState].Nested_Inspect();
            }

            if (_inspectedState == -1)
            {
                for (int i = 0; i < _stateStack.Count; i++)
                {
                    if (Icon.Close.ClickConfirm("ExitState" + i, "Force Exit State"))
                        Exit(_stateStack[i]);
                    else
                        _stateStack[i].InspectInList_Nested(ref _inspectedState, i);

                    pegi.Nl();
                }
            }

            pegi.Nl();

            if (_inspectedState == -1 && "Debug & Test".PegiLabel().IsFoldout(ref _showTest).Nl())
            {
                /*
                "Debug State".PegiLabel().Edit_Enum(ref _debugState);

                if (Icon.Play.Click())
                    _debugState.Enter();
                if (Icon.Exit.Click())
                    _debugState.Exit();*/

                pegi.Nl();

                if (_stateStack.Count > 0 && "Close All".PegiLabel().Click())
                    ManagedOnDisable();
            }
        }
        #endregion



        private class StateMachineCaching
        {
            private int _cachedVersion = -1;
            private readonly Dictionary<Type, object> _cachedResults = new();

            private bool TryGetCached<T>(out T lastValue)
            {
                if (_cachedVersion != Version)
                {
                    lastValue = default(T);
                    return false;
                }

                if (_cachedResults.TryGetValue(typeof(T), out var value))
                {
                    lastValue = (T)value;
                    return true;
                }

                lastValue = default(T);
                return false;
            }

            private void SetCached<T>(T value)
            {
                if (_cachedVersion != Version)
                {
                    _cachedVersion = Version;
                    _cachedResults.Clear();
                }

                _cachedResults[typeof(T)] = value;
            }
        }


        public class StateValue<T>
        {
            private readonly Gate.Integer _stateMachineVersion = new();
            private T _cachedValue;
            private readonly T _defaultValue;
            private bool _isValid;

            /*
            public T GetCurrent
            {
                get
                {
                    if (!_stateMachineVersion.TryChange(Version))
                        return _cachedValue;
                    _cachedValue = Get(defaultValue: _defaultValue);
                    return _cachedValue;
                }
            }*/

            public bool TryGetCurrent(out T curren) 
            {
                TryChangeCurrent(out curren);
                return _isValid;
            }

            public bool TryChangeCurrent() => TryChangeCurrent(out _);
            

            public bool TryChangeCurrent(out T current) 
            {
                if (!_stateMachineVersion.TryChange(Version)) 
                {
                    current = _cachedValue;
                    return false;
                }

                _isValid = TryChangeFallback<T>(ref _cachedValue, _defaultValue);
                current = _cachedValue;
                return _isValid;
            }

            public StateValue(T defaultValue = default)
            {
                _defaultValue = defaultValue;
            }
        }
        

    }

  

    public interface IDataSubtractive<T>
    {
        T Get();
    }

    public interface IDataSubtractive_List<T>
    {
        List<T> Get();
    }

    public interface IDataAdditive<T>
    {
        T Get();
    }

    public interface IDataFallback_Conditional<T>
    {
        bool TryGet(out T value);
    }

    public interface IDataFallback_Break<T> // Will break the loop
    {
    }

    public interface IDataFallback<T>
    {
        T Get();
    }

    
}