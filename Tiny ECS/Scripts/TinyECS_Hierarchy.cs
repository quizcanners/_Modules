using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SearchService;
using UnityEngine.UIElements;

namespace QuizCanners.TinyECS
{
    public static class TinyECS_Hierarchy
    {
        internal static bool _insideUpdateHierarchyLoop;

        internal static HashSet<int> _processedHierarchy = new();

        internal static IEntity _targetEntity;

        internal static HierarchyUpdateStep currentStep;

        private static int _maxDepth;
        private static Action _systemsUpdateLoop;

        internal enum HierarchyUpdateStep { Parents, Children }

        public struct Child 
        {
            public IEntity parent;
            public int orderOfExecution;
        }

        // This will ensure the systems will update in correct order
        public static void UpdateHierarchy<T>(this World<T> world, Action systemsUpdateLoop) where T: ITinyECSworld
        {
            _systemsUpdateLoop = systemsUpdateLoop;
          
            using (QcSharp.DisposableAction(() => _insideUpdateHierarchyLoop = false))
            {
                _processedHierarchy.Clear();
                _maxDepth = 256;

                currentStep = HierarchyUpdateStep.Parents;
                world.WithAll<Child>().Run(UpdateParents);

                currentStep = HierarchyUpdateStep.Children;
                systemsUpdateLoop.Invoke();
            }
        }

        private static void UpdateParents(ref Child child, IEntity ent)
        {
            if (_processedHierarchy.Contains(ent.Index)) //ent.HierarchyUpdateVersion == _hierarchyUpdateVersion)
                return;

            _insideUpdateHierarchyLoop = true;

            _processedHierarchy.Add(ent.Index);

            if (child.parent.IsAlive)
            {
                if (child.parent.TryGetComponent<Child>(out var parentAsChild))
                {
                    if (_maxDepth < 0)
                    {
                        Debug.LogError("Parent Depth exceeded limit. Breaking loop");
                        return;
                    }

                    _maxDepth--;

                    UpdateParents(ref parentAsChild, child.parent);

                    _maxDepth++;
                }

                _targetEntity = ent;

                _systemsUpdateLoop.Invoke();

            }
            else
            {
                ent.Destroy();
            }
        }

    }
}