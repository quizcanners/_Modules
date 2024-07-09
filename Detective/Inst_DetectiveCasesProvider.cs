using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.DetectiveInvestigations
{
    [ExecuteAlways]
    public class Inst_DetectiveCasesProvider : MonoBehaviour, IPEGI
    {
        public List<SO_Detective_Case_Prototype> Cases;


        private void Reset()
        {
            Cases = new List<SO_Detective_Case_Prototype>();
        }

        private void OnEnable()
        {
            Detective.s_CaseProviders.Add(this);
        }

        private void OnDisable() 
        {
            Detective.s_CaseProviders.Remove(this);
        }

        #region Inspector

        public override string ToString() => gameObject.name;

        readonly pegi.CollectionInspectorMeta _casesMeta = new("Cases");
        public void Inspect()
        {
            _casesMeta.Edit_List(Cases).Nl();
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Inst_DetectiveCasesProvider))]
    internal class Inst_DetectiveCasesProviderDrawer : PEGI_Inspector_Override { }
}
