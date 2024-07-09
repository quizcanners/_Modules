using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.DetectiveInvestigations
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + "Detective/" + FILE_NAME)]
    public class SO_Detective_Case_Prototype : ScriptableObject, IPEGI, IGotName
    {
        public const string FILE_NAME = "Detective Case Prototype";

        public string CaseName = "Classified";

        public string Description;

        public InvestigationLeads Leads = new();

        [Serializable]
        public class InvestigationLeads : SerializableDictionary<string, Detective.Lead> { }

        [Serializable]
        public class Id : SmartId.StringGeneric<SO_Detective_Case_Prototype>
        {
            public Id(SO_Detective_Case_Prototype original) 
            {
                Id = original.NameForInspector;
            }

            protected override Dictionary<string, SO_Detective_Case_Prototype> GetEnities() => Detective.GetCases();
        }

        #region Inspector


        public string NameForInspector { get => CaseName; set => CaseName = value; }


        public override string ToString() => "Case File: {0}".F(CaseName);

        [NonSerialized]
        private readonly pegi.CollectionInspectorMeta _leadsListMeta = new(labelName: "Leads");

        public static SO_Detective_Case_Prototype inspected;

        public void Inspect()
        {
            inspected = this;
            if (!_leadsListMeta.IsAnyEntered)
            {
                "Name".PegiLabel(width: 50).Edit(ref CaseName).Nl();
                "Description".PegiLabel().Edit_Big(ref Description).Nl();
            }

            _leadsListMeta.Edit_Dictionary(Leads).Nl();

        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(SO_Detective_Case_Prototype))] internal class SO_Detective_Case_PrototypeDrawer : PEGI_Inspector_Override { }
}