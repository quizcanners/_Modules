using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.DetectiveInvestigations
{
    public static partial class Detective 
    {
        [Serializable]
        public class Lead : IPEGI, IGotName
        {
            public string Key;
            public string Name;

            public LeadProgressStages ThreadProgress = new();


            [Serializable]
            public class LeadProgressStages : SerializableDictionary<string, Progress> { }

            #region Inspector
            public override string ToString() => Name;

            [SerializeField]
            private pegi.CollectionInspectorMeta _progressMeta = new("Progress stages");

            public string NameForInspector { get => Key; set => Key = value; }

            public void Inspect()
            {
                if (!_progressMeta.IsAnyEntered) 
                {
                    "Name".PegiLabel(50).Edit(ref Name).Nl();
                }

                _progressMeta.Edit_Dictionary(ThreadProgress).Nl();

            }
            #endregion

            [Serializable]
            public class Id : SmartId.StringGeneric<Lead>
            {
                [SerializeField] public SO_Detective_Case_Prototype.Id _casCaseId;

                protected override Dictionary<string, Lead> GetEnities()
                {
                    return _casCaseId.GetEntity().Leads;
                }

                public Id(SO_Detective_Case_Prototype investigation, Lead lead) : base(lead)
                {
                    _casCaseId = new SO_Detective_Case_Prototype.Id(investigation);
                }

                public override void Inspect()
                {
                    base.Inspect();

                    if (_casCaseId == null)
                        "Instanciate Case ID".PegiLabel().Click().Nl(() => _casCaseId = new(SO_Detective_Case_Prototype.inspected));
                    else
                        _casCaseId.Nested_Inspect().Nl();
                }
            }


        }

        [Serializable]
        public class Progress : IPEGI, IPEGI_ListInspect, IGotName
        {
            public string Key;

            public List<Lead.Id> UnlockedThreads = new();

            public float TimeNeeded = 1;
            public string Revelation;

            #region Inspector

            public string NameForInspector { get => Key; set => Key = value; }

            private readonly pegi.CollectionInspectorMeta _unlockMeta = new("Unlocked leads", showAddButton: false);

            private Lead selectedLead;

            public void Inspect()
            {
                pegi.Edit(ref TimeNeeded, width: 30);
                pegi.Edit_Big(ref Revelation);

                _unlockMeta.Edit_List(UnlockedThreads).Nl();

                "New Lead".PegiLabel().Select(ref selectedLead, SO_Detective_Case_Prototype.inspected.Leads);

                if (selectedLead != null) 
                {
                    if (!UnlockedThreads.Contains(selectedLead)) 
                    {
                        if (selectedLead.ThreadProgress.ContainsValue(this))
                            "That is a parent Lead".PegiLabel().WriteWarning().Nl();
                        else
                        {
                            if ("Add to Collection".PegiLabel().Click().Nl())
                                UnlockedThreads.Add(new Lead.Id(SO_Detective_Case_Prototype.inspected, selectedLead));
                        }
                    }
                }

            }

            public void InspectInList(ref int edited, int index)
            {
                if (Icon.Enter.Click())
                    edited = index;

                pegi.Edit(ref TimeNeeded, width: 30);
                pegi.Edit(ref Revelation);
            }

            #endregion
        }

    }
}
