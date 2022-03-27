using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCanners.IsItGame
{
    public abstract class EnumeratedAssetListsBase<T, G> : ScriptableObject, IPEGI where T : struct, IComparable, IFormattable, IConvertible where G : Object
    {
        [SerializeField] protected List<EnumeratedObjectList> enumeratedObjects = new List<EnumeratedObjectList>();

        public bool TryGet(T key, out G obj) 
        {
            if (TryGet(key, out EnumeratedObjectList sp))
            {
                obj = sp.list.GetRandom() as G;
                return obj;
            }

            obj = null;
            return false;
        }

        private bool TryGet(T value, out EnumeratedObjectList obj)
        {
            int index = Convert.ToInt32(value);

            if (enumeratedObjects.Count > index)
            {
                obj = enumeratedObjects[index];
                return true;
            }

            obj = null;

            return false;
        }

        #region Inspector

        private int _inspectedList = -1;

        public virtual void Inspect()
        {

           // "Defaul {0}".F(typeof(G).ToPegiStringType()).edit(120, ref defaultAsset, allowSceneObjects: true).nl();

            EnumeratedObjectList.s_InspectedEnum = typeof(T);
            EnumeratedObjectList.s_InspectedObjectType = typeof(G);

            "Enumerated {0}".F(typeof(G).ToPegiStringType()).PegiLabel().Edit_List(enumeratedObjects, ref _inspectedList).Nl();

        }
        #endregion
    }

    [Serializable]
    public class EnumeratedObjectList : IPEGI_ListInspect, IGotReadOnlyName, IPEGI, IGotCount
    {
        [SerializeField] private string nameForInspector = "";
        public List<Object> list;

        #region Inspector
        public static Type s_InspectedEnum;
        public static Type s_InspectedObjectType;

        public void InspectInList(ref int edited, int ind)
        {
            var changeToken = pegi.ChangeTrackStart();

            var name = Enum.ToObject(s_InspectedEnum, ind).ToString();

            if (!nameForInspector.Equals(name))
            {
                nameForInspector = name;
                changeToken.Feed(isChanged: true);
            }

            "{0} [{1}]".F(nameForInspector, GetCount()).PegiLabel().Write();

            if (list == null)
            {
                list = new List<Object>();
                changeToken.Feed(isChanged: true);
            }

            if (list.Count < 2)
            {
                var el = list.TryGet(0);

                if (pegi.Edit(ref el, s_InspectedObjectType, 90))
                    list.ForceSet(0, el);
            }

            if (Icon.Enter.Click())
                edited = ind;
        }

        public int GetCount() => list.IsNullOrEmpty() ? 0 : list.Count;

        public string GetReadOnlyName() => nameForInspector + " " + (list.IsNullOrEmpty() ? "Empty" : pegi.GetNameForInspector(list[0]));

        public void Inspect()
        {
            "All {0}".F(nameForInspector).PegiLabel().Edit_List_UObj(list);
        }
        #endregion
    }
}

