using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCanners.SavageTurret
{

    public abstract class EnumeratedAssetBase<T, G> : ScriptableObject, IPEGI where T : struct, IComparable, IFormattable, IConvertible where G : Object
    {

        public Object defaultAsset;

        [SerializeField] protected List<EnumeratedObject> enumeratedObjects = new List<EnumeratedObject>();

        private bool TryGet(T value, out EnumeratedObject obj)
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

        public virtual G Get(T enumKey)
        {

            return (TryGet(enumKey, out EnumeratedObject sp) ? sp.value : defaultAsset) as G;
        }

        #region Inspector
        public virtual void Inspect()
        {

            "Defaul {0}".F(typeof(G).ToPegiStringType()).PegiLabel(120).Edit(ref defaultAsset, allowSceneObjects: true).Nl();

            EnumeratedObject.inspectedEnum = typeof(T);
            EnumeratedObject.inspectedObjectType = typeof(G);

            "Enumerated {0}".F(typeof(G).ToPegiStringType()).PegiLabel().Edit_List(enumeratedObjects).Nl();
        }
        #endregion
    }

    [Serializable]
    public class EnumeratedObject : IPEGI_ListInspect
    {
        [SerializeField] private string nameForInspector = "";
        public Object value;

        #region Inspector
        public static Type inspectedEnum;
        public static Type inspectedObjectType;

        public void InspectInList(ref int edited, int ind)
        {

            var name = Enum.ToObject(inspectedEnum, ind).ToString();

            if (!nameForInspector.Equals(name))
            {
                nameForInspector = name;
            }

            nameForInspector.PegiLabel(90).Write();

            pegi.Edit(ref value, inspectedObjectType);
        }

        public override string ToString() => nameForInspector + " " + (value ? value.name : ("No " + inspectedObjectType));

        #endregion
    }

}