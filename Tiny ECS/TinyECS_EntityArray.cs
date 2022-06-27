using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public partial class World<W> 
    {
        [Serializable]
        internal class EntityArray : ValidatabeArrayGeneric<Entity>
        {
            public override void TryDestroyFromInspectro(int index) =>
                WorldSingleton<W>.instance.Destroy(_array[index]);

            protected override Entity Revalidate(int index)
            {
                var ent = _array[index];
                ent.Version++;
                ent.Index = index;
                _array[index] = ent;
                return ent;
            }


            protected override IEnumerator<Entity> Get_Enumerator_Internal()
            {
                if (TinyECS_Hierarchy._insideUpdateHierarchyLoop)
                {
                    switch (TinyECS_Hierarchy.currentStep) 
                    {
                        case TinyECS_Hierarchy.HierarchyUpdateStep.Parents:
                            _hierarchy.Reset();
                            return _hierarchy;
                        case TinyECS_Hierarchy.HierarchyUpdateStep.Children:
                            return new Hierarchy_Unapdated(base.Get_Enumerator_Internal());
                        default:
                            throw new Exception("Hierarchy step not implemented: {0}".F(TinyECS_Hierarchy.currentStep));
                    }
                }
                else
                {
                    return base.Get_Enumerator_Internal();
                }
            }

            private Hierarchy_SingleTarget _hierarchy = new();

            private class Hierarchy_SingleTarget : IEnumerator<Entity>
            {
                private bool currentProcessed;

                public bool MoveNext()
                { 
                    if (currentProcessed)
                        return false;

                    currentProcessed = true;

                    return true;
                }

                public void Reset()
                {
                    currentProcessed = false;
                }

                public void Dispose()
                {
                    currentProcessed = false;
                }

                public Entity Current => (Entity)TinyECS_Hierarchy._targetEntity;

                object IEnumerator.Current => TinyECS_Hierarchy._targetEntity;
            }

            private class Hierarchy_Unapdated : IEnumerator<Entity>
            {

                IEnumerator<Entity> _mainEnumerator;

                public bool MoveNext()
                {
                    bool canMove;
                    do
                    {
                        canMove = _mainEnumerator.MoveNext();
                    } while (canMove && TinyECS_Hierarchy._processedHierarchy.Contains(_mainEnumerator.Current.Index));
                    // If objct was Updated as a Singl target, we move on to next one


                    return canMove;
                }

                public void Reset()
                {
                    _mainEnumerator.Reset();
                }

                public void Dispose()
                {
                    _mainEnumerator.Dispose();
                }

                public Entity Current => _mainEnumerator.Current;

                object IEnumerator.Current => _mainEnumerator.Current;

                public Hierarchy_Unapdated(IEnumerator<Entity> mainEnumerator)
                {
                    _mainEnumerator = mainEnumerator;
                }
            }
        }
    }
}