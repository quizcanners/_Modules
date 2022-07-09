using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public interface IEntity : IPEGI, IGotName
    {
        int Index { get; }
        int Version { get; }

        public bool IsAlive { get; }

        bool HasComponent<T>() where T : struct, IComponentData;
        IEntity AddComponent<T>() where T : struct, IComponentData;
        IEntity AddComponent<T>(SystemActionR<T> onCreate) where T : struct, IComponentData;

        void SetComponent<T>(T data) where T : struct, IComponentData;
        T GetComponent<T>() where T : struct, IComponentData;

        void InspectComponent<T>() where T : struct, IComponentData;
        public void Destroy();
        public ITinyECSworld WorldLink { get; }
    }

    public partial class World<W> 
    {
        public struct Entity : IEntity, IPEGI_ListInspect, IPEGI
        {
            internal int Index;
            internal int Version;

            int IEntity.Index => Index;
            int IEntity.Version => Version;

            public bool IsAlive => World.IsAlive(this);

            public bool HasComponent<T>() where T : struct, IComponentData
                => World.HasComponent<T>(this);

            public IEntity AddComponent<T>() where T : struct, IComponentData
            {
                World.AddComponent<T>(this);
                return this;
            }

            public IEntity AddComponent<T>(SystemActionR<T> onCreate) where T : struct, IComponentData
            {
                World.AddComponent(this, onCreate);
                return this;
            }

            public void RemoveComponent<T>() where T : struct, IComponentData
                => World.RemoveComponent<T>(this);

            public T GetComponent<T>() where T : struct, IComponentData
            {
#if UNITY_EDITOR
                if (World == null) 
                {
                    Debug.LogError("World {0} is null".F(typeof(W)));
                    return default(T);
                }
#endif

                return World.GetComponent<T>(this);
            }

            public void SetComponent<T>(T data) where T : struct, IComponentData => World.SetComponent(this, data);
            
            public void Destroy() => World.Destroy(this);

            public bool TryGetComponent<T>(out T component) where T : struct, IComponentData
                => World.TryGetComponent(this, out component);

            internal bool TryGetComponentIndex<T>(out ComponentIndex index) where T : struct, IComponentData
                => World.TryGetComponentIndex<T>(this, out index);

            public ITinyECSworld WorldLink => World != null ? World.link : null;

            private World<W> World => WorldSingleton<W>.instance;

            #region Inspector

            public void InspectInList(ref int edited, int index)
            {
                (IsAlive ? Icon.Active : Icon.InActive).Draw();

                "{0} [{1}]".F(Index, Version).PegiLabel(60).Write();

                pegi.inspect_Name(this);

                if (Icon.Enter.Click())
                    edited = index;
            }

            public void Inspect() 
            {
                var world = WorldLink;

                if (world == null) 
                {
                    "No World Link".PegiLabel().Nl();
                    return;
                }

                if (!IsAlive) 
                {
                    "Entity is Disposed".PegiLabel().Nl();
                    return;
                }

                world.Inspect(this);

                if (World.componentListsForEntity.TryGetValue(Index, out EntityComponentsList components))
                    pegi.Nested_Inspect(ref components).Nl();
            }
               
            public void InspectComponent<T>() where T : struct, IComponentData
            {

                if (!IsAlive)
                {
                    return;
                }

                var has = HasComponent<T>();

                if (has)
                {
                    if (Icon.Delete.Click())
                        RemoveComponent<T>();
                    else
                    {
                        var change = pegi.ChangeTrackStart();
                     
                        IPEGI_ListInspect cmp = GetComponent<T>() as IPEGI_ListInspect;

                        if (cmp != null)
                            pegi.Inspect_AsInList_Value(ref cmp);
                        else
                            typeof(T).ToPegiStringType().PegiLabel().Write();

                        if (change)
                            SetComponent((T)cmp);
                    }
                }

                if (!has)
                {
                    if (Icon.Add.Click())
                        AddComponent<T>();

                    typeof(T).ToPegiStringType().PegiLabel().Write();
                }

                pegi.Nl();

            }
            #endregion

            #region Comparison

            public bool Equals(Entity obj) =>
                 Index == obj.Index && Version == obj.Version;

            public override bool Equals(object obj)
            {
                if (!(obj is Entity))
                    return false;

                return Equals((Entity)obj);
            }

            public override int GetHashCode() => Index * 100000 + Version;


            public string NameForInspector { get => World.GetName(this); set => World.SetName(this, value); }

            #endregion
        }
    }
}