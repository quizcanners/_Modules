using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class C_PriorityComponentSingleton : MonoBehaviour, IPEGI
    {
        [SerializeField] private Component _component;
        [SerializeField] private int _priority;

        private static Dictionary<Type, List<C_PriorityComponentSingleton>> allListeners = new Dictionary<Type, List<C_PriorityComponentSingleton>>();

        private List<C_PriorityComponentSingleton> GetAllByType() => allListeners.GetOrCreate(_component.GetType());

        private LoopLock loopLock = new LoopLock();

        private void UpdateMain()
        {
            if (!loopLock.Unlocked)
                return;

            using (loopLock.Lock())
            {
                var lst = GetAllByType();

                int highestPriority = int.MinValue;
                C_PriorityComponentSingleton main = null;

                foreach (var l in lst)
                {
                    if (l.gameObject && l._priority > highestPriority)
                    {
                        highestPriority = l._priority;
                        main = l;
                    }
                }

                foreach (var l in lst)
                {
                    if (l._component.gameObject)
                        l._component.gameObject.SetActive(l == main);
                }
            }
        }

        void OnEnable() 
        {
            if (GetAllByType().Contains(this) == false)
                GetAllByType().Add(this);
            UpdateMain();
        }

        private void OnDisable()
        {
            GetAllByType().Remove(this);
            UpdateMain();
        }

        private void OnDestroy()
        {
            GetAllByType().Remove(this);
            UpdateMain();
        }

        public void Inspect()
        {
            Icon.Refresh.Click(UpdateMain);

            pegi.Nl();

           

            "Component".PegiLabel(80).Edit(ref _component).Nl();
            "Priority".PegiLabel(80).Edit(ref _priority).Nl();

            if (!_component)
                "Assign component to know which type to compete with".PegiLabel().Write_Hint();
            else
            {
                if (!_component.transform.IsChildOf(transform))
                    "The singleton Component should be a child of this component".PegiLabel().WriteWarning();
                else
                {
                  
                    "{0}s with lower priority will be disabled".F(_component.gameObject.name).PegiLabel().Write_Hint();
                    var optimalName = QcSharp.AddSpacesInsteadOfCapitals("{0} Singleton".F(_component.GetType().ToPegiStringType()));

                    if (_component.gameObject.name.Equals(optimalName) == false && "Set Name".PegiLabel().Click().Nl())
                        _component.gameObject.name = optimalName;
                }
                if (Application.isPlaying) 
                {
                    var all = GetAllByType();

                    if (all.Count > 1) 
                    {
                        "All {0}".F(_component.GetType()).PegiLabel().Edit_List(GetAllByType()).Nl();
                    }
                }
            }
        }
    }

    [PEGI_Inspector_Override(typeof(C_PriorityComponentSingleton))] internal class C_PriorityComponentSingletonDrawer : PEGI_Inspector_Override { }
}