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

        bool HasComponent<T>() where T : struct, IComponentData;
        void AddComponent<T>() where T : struct, IComponentData;
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

            public void AddComponent<T>() where T : struct, IComponentData
                => World.AddComponent<T>(this);

            public void AddComponent<T>(SystemActionR<T> onCreate) where T : struct, IComponentData
                => World.AddComponent(this, onCreate);

            public void RemoveComponent<T>() where T : struct, IComponentData
                => World.RemoveComponent<T>(this);

            public T GetComponent<T>() where T : struct, IComponentData
               => World.GetComponent<T>(this);

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

                world.Inspect(this);

                if (World.componentListsForEntity.TryGetValue(Index, out EntityComponentsList components))
                    pegi.Nested_Inspect(ref components).Nl();
            }
               
            public void InspectComponent<T>() where T : struct, IComponentData
            {
                var has = HasComponent<T>();
                if (has)
                {
                    if (Icon.Delete.Click())
                        RemoveComponent<T>();

                    var cmp = GetComponent<T>() as IPEGI_ListInspect;

                    if (cmp != null)
                        cmp.InspectInList_Nested();
                    else
                        typeof(T).ToPegiStringType().PegiLabel().Write();
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