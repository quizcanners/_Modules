using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;


namespace QuizCanners.TinyECS
{
    public partial class World<W>
    {
        [Serializable]
        internal class ComponentArraysDictionary : SerializableDictionary<int, ComponentCollectionBase> { }

        internal abstract class ComponentCollectionBase : IGotCount
        {
            internal abstract object GetComponentObject(ComponentIndex index);
            internal abstract ComponentIndex Create();
            internal abstract bool TryDestroy(ComponentIndex entity);
            public abstract int GetCount();
        }

        internal class ComponentArrayGenric<T> : ComponentCollectionBase, IPEGI_ListInspect, IPEGI, IGotReadOnlyName where T : IComponentData
        {
            internal ComponentArray components = new ComponentArray();

            internal override object GetComponentObject(ComponentIndex index) => components[index.Index];

            internal T this[ComponentIndex index]
            {
                get => components[index.Index];
                set => components[index.Index] = value;
            }

            internal override ComponentIndex Create()
            {
                components.Create(out int index);
                return new ComponentIndex(index);
            }

            internal ComponentIndex Create(SystemActionR<T> action)
            {
                var result = components.Create(out int index);
                action.Invoke(ref result);
                components[index] = result;
                return new ComponentIndex(index);
            }

            internal override bool TryDestroy(ComponentIndex index) => components.TryDestroy(index.Index);

            internal class ComponentArray : ValidatabeArrayGeneric<T>
            {
                protected override T Revalidate(int index)
                {
                    return _array[index];
                }
            }

            #region Inspector

            public void InspectInList(ref int edited, int index)
            {
                GetReadOnlyName().PegiLabel().Write();
                if (Icon.Enter.Click())
                    edited = index;
            }
            public void Inspect() => components.Nested_Inspect();
            public string GetReadOnlyName() => "{0} <{1}>".F(typeof(T).ToPegiStringType(), components == null ? "NULL" : components.GetCount());
            public override int GetCount() => components == null ? 0 : components.GetValidatedCount();
            #endregion
        }

 
        [Serializable]
        internal struct EntityComponentsList : IPEGI
        {
            private int componentFlags;
            private Dictionary<int, ComponentIndex> _componentIndexes;

            internal ComponentIndex this[int typeFlag] => _componentIndexes[typeFlag];
            internal bool HasComponents(int subsetFlags) => (componentFlags & subsetFlags) == subsetFlags;
            internal bool HasComponent<T>() where T : struct, IComponentData
                => _componentIndexes == null ? false : _componentIndexes.TryGetValue(World.GetFlag<T>(), out _);

            internal void AddComponent<T>(SystemActionR<T> onCreate) where T : struct, IComponentData
            {
                var flag = World.GetFlag<T>();

                if (_componentIndexes == null)
                    _componentIndexes = new Dictionary<int, ComponentIndex>();

                ComponentIndex ind = World.GetComponentDatas<T>().Create(onCreate);

                _componentIndexes[flag] = ind;

                componentFlags |= World.GetFlag<T>();
            }

            internal ComponentIndex AddComponent<T>() where T : struct, IComponentData
            {
                var flag = World.GetFlag<T>(); 

                if (_componentIndexes == null)
                    _componentIndexes = new Dictionary<int, ComponentIndex>();

                ComponentIndex ind = World.GetComponentDatas<T>().Create();

                _componentIndexes[flag] = ind;

                componentFlags |= World.GetFlag<T>();

                return ind;
            }


            internal void OnDestroy() 
            {
                if (_componentIndexes == null)
                    return;

                var cmps = World.allComponents;

                foreach (var c in _componentIndexes)
                    cmps[c.Key].TryDestroy(c.Value);

                _componentIndexes.Clear();
                componentFlags = 0;
            }
            internal void Remove<T>() where T : struct, IComponentData => Remove_Internal(World.GetFlag<T>());
            public bool TryGet<T>(out ComponentIndex value) where T : struct, IComponentData
            {
                if (_componentIndexes == null)
                {
                    value = default(ComponentIndex);
                    return false;
                }

                return _componentIndexes.TryGetValue(World.GetFlag<T>(), out value);
            }

            private World<W> World => WorldSingleton<W>.instance;

            private bool Remove_Internal(int flag) 
            {
                if ((componentFlags & flag) == 0) //_componentIndexes == null || !_componentIndexes.TryGetValue(type, out var index))
                {
                    return false;
                }

                componentFlags ^= flag;

                _componentIndexes.TryGetValue(flag, out var index);
                _componentIndexes.Remove(flag);

                World.allComponents[flag].TryDestroy(index);

                return true;
            }

            #region Inspector

            public void Inspect()
            {
                if (_componentIndexes == null)
                    "Empty".PegiLabel().Nl();
                else
                {
                    ComponentArraysDictionary allComponent = World.allComponents;

                    foreach (KeyValuePair<int, ComponentIndex> pair in _componentIndexes)
                    {
                        int flag = pair.Key;

                        bool hasFlag = (componentFlags & flag) != 0;

                        if (hasFlag)
                            Icon.Done.Draw();
                        else
                            Icon.Close.Draw();

                        allComponent[pair.Key].GetComponentObject(pair.Value).GetNameForInspector().PegiLabel().Write();

                        pegi.Nl();
                    }

                    Convert.ToString(componentFlags, toBase: 2).PegiLabel().Nl();
                }
            }
            #endregion
        }
    }

    [Serializable]
    internal struct ComponentIndex
    {
        public int Index;

        public ComponentIndex(int index)
        {
            Index = index;
        }
    }

}