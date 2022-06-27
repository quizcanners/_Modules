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

        public int VariantsCount(T key) 
        {
            if (TryGet(key, out EnumeratedObjectList sp))
                return sp.list.Count;

            return 0;
        } 

        public bool TryGet(T key, out G obj) 
        {
            if (TryGet(key, out EnumeratedObjectList sp))
            {
                obj = sp.GetRandom() as G;
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

        private readonly pegi.CollectionInspectorMeta _listMeta = new("Enumerated {0}".F(typeof(G).ToPegiStringType()));

        public virtual void Inspect()
        {

           // "Defaul {0}".F(typeof(G).ToPegiStringType()).edit(120, ref defaultAsset, allowSceneObjects: true).nl();

            EnumeratedObjectList.s_InspectedEnum = typeof(T);
            EnumeratedObjectList.s_InspectedObjectType = typeof(G);

            _listMeta.Edit_List(enumeratedObjects).Nl();

        }
        #endregion
    }

    [Serializable]
    public class EnumeratedObjectList : IPEGI_ListInspect, IPEGI, IGotCount, INeedAttention
    {
        [SerializeField] private string nameForInspector = "";
        public List<Object> list;

        private int _previousRandom = -1;

        public Object GetRandom() => list.GetRandom(ref _previousRandom);


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

            "{0} [{1}]".F(nameForInspector, GetCount()).PegiLabel(0.33f).Write();

            if (list == null)
            {
                list = new List<Object>();
                changeToken.Feed(isChanged: true);
            }

            if (list.Count < 2)
            {
                var el = list.TryGet(0);

                if (pegi.Edit(ref el, s_InspectedObjectType))
                    list.ForceSet(0, el);
            }

            if (pegi.Click_Enter_Attention(this))
                edited = ind;
        }

        public int GetCount() => list.IsNullOrEmpty() ? 0 : list.Count;

        public override string ToString() => nameForInspector + " " + (list.IsNullOrEmpty() ? "Empty" : pegi.GetNameForInspector(list[0]));

        private readonly pegi.CollectionInspectorMeta _listMeta = new("All");

        public void Inspect()
        {
            _listMeta.Edit_List_UObj(list);
        }

        public string NeedAttention()
        {
            if (pegi.NeedsAttention(list, out var message, "Sounds", canBeNull: false)) 
            {
                return message;
            }

            return null;
        }
        #endregion
    }
}

