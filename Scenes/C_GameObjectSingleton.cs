using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class C_GameObjectSingleton : MonoBehaviour
    {
        [SerializeField] private SingletonRole _role;
        [SerializeField] private SingletonConflictSolution _conflictResolve = SingletonConflictSolution.DestroyThis;

        private enum SingletonConflictSolution 
        {
            DestroyThis, DestroyPrevious
        }

        private enum SingletonRole 
        { 
            MainCamera = 0, 
            Canvas = 1,
            Player = 2,
            DirectionalLight = 3,
            EventSystem = 4,
            EffectManagers = 5,
            AudioListener = 6,
            UiCamera = 7,
        }

        private static readonly Dictionary<SingletonRole, GameObject> _inTheScene = new();

        private void Awake()
        {
            CheckSingleton();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            CheckSingleton();
        }
#endif

        private void CheckSingleton() 
        {
            if (_inTheScene.TryGetValue(_role, out GameObject go) && go && go != gameObject)
            {
                switch (_conflictResolve) 
                {
                    case SingletonConflictSolution.DestroyThis: Destroy(gameObject); break;
                    case SingletonConflictSolution.DestroyPrevious: Destroy(go); _inTheScene[_role] = gameObject; break;
                }
               
            }
            else
            {
                _inTheScene[_role] = gameObject;
            }
        }

    }
}
