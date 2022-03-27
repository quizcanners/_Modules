using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public delegate void SystemActionR<T>(ref T item);
    public delegate void SystemActionRR<T1, T2>(ref T1 item1, ref T2 item2);
    public delegate void SystemActionRI<T1, T2>(ref T1 item1, T2 item2);
    public delegate void SystemActionII<T1, T2>(T1 item1, T2 item2);
    public delegate void SystemActionEntityRRE<T1, T2>(ref T1 item1, ref T2 item2, IEntity entity);

    public partial class World<W>
    {
        public void RunSystem<T>(SystemActionR<T> action) where T : struct, IComponentData
        {
            var flag = GetFlag<T>();

            if (!allComponents.TryGetValue(flag, out var cmps))
                return;

            var tComps = (cmps as ComponentArrayGenric<T>).components;

            foreach (Entity e in allEntities) 
            {
                EntityComponentsList componentsList = this[e];

                if (!componentsList.HasComponents(flag))
                    continue;

                var index = componentsList[flag].Index;

                var el = tComps[index];

                action.Invoke(ref el);

                tComps[index] = el;
            }
        }
        public void RunSystem<T1, T2>(SystemActionR<T1> action)
         where T1 : struct, IComponentData
         where T2 : struct, IComponentData
        {

            var flagOne = GetFlag<T1>();

            if (!allComponents.TryGetValue(flagOne, out var cmps1))
                return;

            ComponentArrayGenric<T1>.ComponentArray tComps1 = (cmps1 as ComponentArrayGenric<T1>).components;

            int componentFLags = GetFlag<T1>() | GetFlag<T2>();

            foreach (Entity e in allEntities)
            {
                EntityComponentsList list = this[e];

                if (!list.HasComponents(componentFLags))
                    continue;

                ComponentIndex index1 = list[flagOne];
                var el1 = tComps1[index1.Index];

                action.Invoke(ref el1);

                tComps1[index1.Index] = el1;
            }
        }

        public void RunSystem<T1, T2>(SystemActionRR<T1, T2> action) 
            where T1 : struct, IComponentData
            where T2 : struct, IComponentData
        {
            if (!TryGet<T1, T2>(out var tComps1, out var tComps2, out int flagOne, out int flagTwo))
                return;

            int componentFLags = flagOne | flagTwo;

            foreach (Entity e in allEntities)
            {
                EntityComponentsList componentsList = this[e];

                if (!componentsList.HasComponents(componentFLags))
                    continue;

                var index1 = componentsList[flagOne].Index;
                var index2 = componentsList[flagTwo].Index;

                var el1 = tComps1[index1];
                var el2 = tComps2[index2];

                action.Invoke(ref el1, ref el2);

                tComps1[index1] = el1;
                tComps2[index2] = el2;
            }
        }

        public void RunSystem<T1, T2>(SystemActionRI<T1, T2> action)
            where T1 : struct, IComponentData
            where T2 : struct, IComponentData
        {
            if (!TryGet<T1, T2>(out var tComps1, out var tComps2, out int flagOne, out int flagTwo))
                return;

            int componentFLags = flagOne | flagTwo;

            foreach (Entity e in allEntities)
            {
                EntityComponentsList componentsList = this[e];

                if (!componentsList.HasComponents(componentFLags))
                    continue;

                var index1 = componentsList[flagOne].Index;
                var index2 = componentsList[flagTwo].Index;

                var el1 = tComps1[index1];
                var el2 = tComps2[index2];

                action.Invoke(ref el1, el2);

                tComps1[index1] = el1;
            }
        }

        public void RunSystem<T1, T2>(SystemActionII<T1, T2> action)
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        {
            if (!TryGet<T1, T2>(out var tComps1, out var tComps2, out int flagOne, out int flagTwo))
                return;

            int componentFLags = flagOne | flagTwo;

            foreach (Entity e in allEntities)
            {
                EntityComponentsList componentsList = this[e];

                if (!componentsList.HasComponents(componentFLags))
                    continue;

                var index1 = componentsList[flagOne].Index;
                var index2 = componentsList[flagTwo].Index;

                var el1 = tComps1[index1];
                var el2 = tComps2[index2];

                action.Invoke(el1, el2);
            }
        }

        public void RunSystem<T1, T2>(SystemActionEntityRRE<T1, T2> action)
         where T1 : struct, IComponentData
         where T2 : struct, IComponentData
        {
            if (!TryGet<T1, T2>(out var tComps1, out var tComps2, out int flagOne, out int flagTwo))
                return;

            int componentFLags = GetFlag<T1>() | GetFlag<T2>();

            foreach (Entity e in allEntities)
            {
                EntityComponentsList componentsList = this[e];

                if (!componentsList.HasComponents(componentFLags))
                    continue;

                var index1 = componentsList[flagOne].Index;
                var index2 = componentsList[flagTwo].Index;

                var el1 = tComps1[index1];
                var el2 = tComps2[index2];

                action.Invoke(ref el1, ref el2, e);

                tComps1[index1] = el1;
                tComps2[index2] = el2;
            }
        }

        private bool TryGet<T1, T2>(
        out ComponentArrayGenric<T1>.ComponentArray tComps1,
        out ComponentArrayGenric<T2>.ComponentArray tComps2,
        out int flagOne, out int flagTwo
        )
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        {
            flagOne = GetFlag<T1>();
            flagTwo = GetFlag<T2>();

            if (!allComponents.TryGetValue(flagOne, out var cmps1)
                || !allComponents.TryGetValue(flagTwo, out var cmps2))
            {
                tComps1 = null;
                tComps2 = null;
                return false;
            }

            tComps1 = (cmps1 as ComponentArrayGenric<T1>).components;
            tComps2 = (cmps2 as ComponentArrayGenric<T2>).components;
            return true;
        }
    }

}