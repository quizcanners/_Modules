using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.IsItGame.Triggers
{
    [ExecuteAlways]
    public class Singleton_TriggerValues : Singleton.BehaniourBase, IPEGI
    {
        public const string TRIGGERS = "Triggers";

        [SerializeField] internal TriggerGroupsDictionary triggerGroup = new();
        public int Version;
        public TriggerValues Values = new();

        internal SO_TriggerGroupMeta.TriggerMeta this[IIntTriggerIndex index]
        {
            get
            {
                if (!index.IsValid())
                    return null;

                if (triggerGroup.TryGetValue(index.GetGroupId(), out var group))
                    return group[index];

                return null;
            }
        }

        internal SO_TriggerGroupMeta.TriggerMeta this[IBoolTriggerIndex index]
        {
            get
            {
                if (!index.IsValid())
                    return null;

                if (triggerGroup.TryGetValue(index.GetGroupId(), out var group))
                    return group[index];

                return null;
            }
        }

        #region Inspector

        public override string InspectedCategory => Utils.Singleton.Categories.GAME_LOGIC;

        private pegi.EnterExitContext context = new pegi.EnterExitContext();
        private int _inspectedGroup = -1;
        public override void Inspect()
        {
            using (context.StartContext())
            {
                pegi.Nl();
                "Values".PegiLabel().Enter_Inspect(Values).Nl();
                "Trigger Groups".PegiLabel().Enter_Dictionary(triggerGroup, ref _inspectedGroup).Nl();
            }
        }

        #endregion


        [Serializable]
        internal class TriggerGroupsDictionary : SerializableDictionary<string, SO_TriggerGroupMeta> { }

    }

    [PEGI_Inspector_Override(typeof(Singleton_TriggerValues))] internal class TriggerValuesServiceDrawer : PEGI_Inspector_Override { }
}
