using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.SavageTurret.Triggers
{

    [CreateAssetMenu(fileName = FILE_NAME+ ".triggers", menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_TriggerValues.TRIGGERS + "/" + FILE_NAME)]
    public class SO_TriggerGroupMeta : ScriptableObject, IPEGI, IGotName
    {
        public const string FILE_NAME = "Triggers Meta";

        [SerializeField] internal TriggerDictionary booleans = new();
        [SerializeField] internal TriggerDictionary ints = new();

        public string NameForInspector { get => name; set => QcUnity.RenameAsset(this, value); }

        public void Clear()
        {
            booleans.Clear();
            ints.Clear();
        }

        public int GetCount() => booleans.Count + ints.Count;

        internal TriggerMeta this[IIntTriggerIndex index]
        {
            get => ints.GetOrCreate(index.GetTriggerId());
            set => ints[index.GetTriggerId()] = value;
        }

        internal TriggerMeta this[IBoolTriggerIndex index]
        {
            get => booleans.GetOrCreate(index.GetTriggerId());
            set => booleans[index.GetTriggerId()] = value;
        }

        internal TriggerDictionary GetDictionary(ITriggerIndex index) => index.IsBooleanValue ? booleans : ints;

        #region Inspector

        private readonly pegi.EnterExitContext context = new();
        //private int _inspectedStuff = -1;
        void IPEGI.Inspect()
        {
            using (context.StartContext())
            {
                pegi.Nl();
                "Booleans".PegiLabel().Enter_Inspect(booleans).Nl();
                "Integers".PegiLabel().Enter_Inspect(ints).Nl();
            }
        }

        public void InspectInList(ref int edited, int index)
        {
            if (Icon.Clear.ClickConfirm(confirmationTag: "Erase " + index, "Erase all valus from the group?"))
                Clear();

            if (Icon.Enter.Click())
                edited = index;
        }
        #endregion

        [Serializable] internal class TriggerDictionary : SerializableDictionary<string, TriggerMeta> { }

        [Serializable]
        public class TriggerMeta : IPEGI_ListInspect
        {
            [SerializeField] private string hint = "";

            public void InspectInList(ref int edited, int index)
            {
                pegi.Edit(ref hint);
            }
        }
    }

    [PEGI_Inspector_Override(typeof(SO_TriggerGroupMeta))] internal class TriggerValuesKeyCollectionDrawer : PEGI_Inspector_Override { }
}