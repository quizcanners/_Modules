using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public partial class World<W> : IPEGI, IGotCount where W : ITinyECSworld
    {
        internal EntityArray allEntities = new();
        internal ComponentArraysDictionary allComponents = new();

        internal EntityComponentsList[] componentListsForEntity = new EntityComponentsList[1];
        internal Dictionary<Type, int> componentFlagArray = new();
        internal byte LatestComponentFlag { get; private set; } = 1;

        [Header("For Inspector:")]
        [SerializeField] private string[] _entityNames;
        internal ITinyECSworld link;

        internal ComponentArrayGenric<T> GetComponentDatas<T>() where T : struct
        {
            var flag = GetFlag<T>();

            if (allComponents.TryGetValue(flag, out ComponentCollectionBase components))
                return components as ComponentArrayGenric<T>;

            ComponentArrayGenric<T> result = new();
            allComponents[flag] = result;
            return result;
         

        }

        public Entity CreateEntity() => allEntities.Create(out _);
        public Entity CreateEntity(string name)
        {
            var ent = allEntities.Create(out _);
            ent.NameForInspector = name;
            return ent;
        }

        public void Destroy(Entity entity)
        {
            if (IsAlive(entity))
            {
                var lst = this[entity];
                lst.OnDestroy();
                this[entity] = lst;
                allEntities.TryDestroy(entity.Index);
            }
        }

        internal bool IsAlive(Entity entity) 
            => allEntities.IsValid(entity.Index)
            && allEntities[entity.Index].Version == entity.Version;

        public void ClearWorld() 
        {
            allEntities = new EntityArray();
            allComponents = new ComponentArraysDictionary();
            componentListsForEntity = new EntityComponentsList[1];
            componentFlagArray = new Dictionary<Type, int>();
            LatestComponentFlag = 1;
            _entityNames = null;
        }

        internal int GetFlag<T>() where T : struct => GetFlag(typeof(T));

        internal int GetFlag(Type type)
        {
            if (!componentFlagArray.TryGetValue(type, out var flag))
            {
                flag = 1 << LatestComponentFlag;
                LatestComponentFlag++;
                componentFlagArray[type] = flag;
            }

            return flag;
        }

        internal void AddComponent<T>(Entity entity) where T : struct
        {
            EntityComponentsList list = this[entity];
            list.AddComponent<T>();
            this[entity] = list;
        }

        internal void AddComponent<T>(Entity entity, SystemActionR<T> onCreate) where T : struct
        {
            EntityComponentsList entityComponents = this[entity];
            entityComponents.AddComponent(onCreate);
            this[entity] = entityComponents;
        }

        internal void RemoveComponent<T>(Entity entity) where T : struct
        {
            if (!HasComponent<T>(entity))
            {
                if (Debug.isDebugBuild)
                    QcLog.ChillLogger.LogErrosExpOnly(() => "Entity {0} doesn't have component {1}".F(entity.NameForInspector, typeof(T).Name), key: "DuplCmp" + typeof(T).Name);

                return;
            }

            EntityComponentsList list = this[entity];
            list.Remove<T>();
            this[entity] = list;

        }
        internal EntityComponentsList this[Entity entity] 
        {
            get 
            {
                if (componentListsForEntity.Length <= entity.Index)
                    QcSharp.Resize(ref componentListsForEntity, allEntities.Length);

                return componentListsForEntity[entity.Index];
            }

            set 
            {
                componentListsForEntity[entity.Index] = value;
            }
        }

        internal T GetOrCreateComponent<T>(Entity entity) where T : struct
        {
            EntityComponentsList componentIndexes = this[entity];
            if (componentIndexes.TryGet<T>(out var componentIndex))
            {
                ComponentCollectionBase componentInstances = allComponents[GetFlag<T>()];
                return (T)componentInstances.GetComponentObject(componentIndex);
            }

            var ind = componentIndexes.AddComponent<T>();
            this[entity] = componentIndexes;
            return (T)allComponents[GetFlag<T>()].GetComponentObject(componentIndex);
        }

        internal T GetComponent<T>(Entity entity) where T : struct
        {
            EntityComponentsList cmps = componentListsForEntity[entity.Index];
            cmps.TryGet<T>(out var index); 
            ComponentCollectionBase byType = allComponents[GetFlag<T>()];
            return (T)byType.GetComponentObject(index);
        }

        internal void SetComponent<T>(Entity entity, T componentData) where T : struct 
        {
            EntityComponentsList entityComponents = this[entity];
            ComponentArrayGenric<T> allComponents = GetComponentDatas<T>();
            if (entityComponents.TryGet<T>(out var index)) 
            {
                allComponents[index] = componentData;
            } else 
            {
                entityComponents.AddComponent(componentData);
                this[entity] = entityComponents;
            }
        }

        internal bool TryGetComponent<T>(Entity entity, out T component) where T : struct
        {
            EntityComponentsList cmps = componentListsForEntity[entity.Index];
            if (!cmps.TryGet<T>(out var index))
            {
                component = default;
                return false;
            }

            ComponentCollectionBase byType = allComponents[GetFlag<T>()];
            component = (T)byType.GetComponentObject(index);
            return true;
        }

        internal bool TryGetComponentIndex<T>(Entity entity, out ComponentIndex component) where T : struct
        {
            EntityComponentsList cmps = componentListsForEntity[entity.Index];
            return cmps.TryGet<T>(out component);
        }

        internal bool HasComponent<T>(Entity entity) where T : struct
        {
            if (componentListsForEntity.Length > entity.Index)
                return componentListsForEntity[entity.Index].HasComponent<T>();

            return false;
        }
       
        internal World(ITinyECSworld controller)
        {
            link = controller;
        }

        #region Inspector

        private string DefaultEntityName(Entity entity) => "{0} Entity {1}".F(link.WorldName, entity.Index.ToString());

        public string GetName(Entity entity) => _entityNames.TryGet(index: entity.Index, defaultValue: DefaultEntityName(entity));
        public void SetName(Entity entity, string name)
        {
            if (_entityNames == null)
            {
                _entityNames = new string[entity.Index + 1];
                for (int i = 0; i < entity.Index; i++)
                    _entityNames[i] = DefaultEntityName(new Entity() { Index = i });
            } else if (_entityNames.Length <= entity.Index) 
            {
                QcSharp.Resize(ref _entityNames, allEntities.Length);
            }



            _entityNames[entity.Index] = name;
        }

        private int CountAllComponents() 
        {
            if (allComponents == null)
                return 0;

            int cnt = 0;

            foreach (KeyValuePair<int, ComponentCollectionBase> c in allComponents) 
            {
                cnt += c.Value.GetCount();
            }

            return cnt;
        }

        private readonly pegi.EnterExitContext _context = new();
        private readonly pegi.CollectionInspectorMeta _componentsCollection = new("Components", showAddButton: false, allowDeleting: false, showEditListButton: false);

        public void Inspect()
        {
       
            using (_context.StartContext()) 
            {
                if (_context.IsAnyEntered == false)
                    "{0} World".F(link.WorldName).PegiLabel(pegi.Styles.ListLabel).Nl();

                allEntities.Enter_Inspect().Nl();
             
                _componentsCollection.Enter_Dictionary(allComponents).Nl();
            }
        }

        public override string ToString() => "{0} [{1} x {2}]".F(link.WorldName, allEntities.GetValidatedCount(), CountAllComponents());

        public int GetCount() => allEntities.GetValidatedCount();

        #endregion


  

    }
}