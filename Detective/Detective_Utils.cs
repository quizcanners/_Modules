using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;

namespace QuizCanners.DetectiveInvestigations
{
    public static partial class Detective
    {
        public static List<Inst_DetectiveCasesProvider> s_CaseProviders = new();

        internal static void OnCasesListChanged() => _casesDirty.CreateRequest();

        private static readonly LogicWrappers.Request _casesDirty = new();

        private static readonly Dictionary<string, SO_Detective_Case_Prototype> _casesSortedCached = new();

        public static Dictionary<string, SO_Detective_Case_Prototype> GetCases() 
        {
            if (!_casesDirty.TryUseRequest())
                return _casesSortedCached;

            foreach (var cp in s_CaseProviders)
                foreach (var c in cp.Cases)
                    _casesSortedCached.Add(c.CaseName, c);

            return _casesSortedCached;
        }




        #region Inspector

        private static readonly pegi.EnterExitContext _context = new();

        public static void Inspect() 
        {
            using (_context.StartContext())
                foreach (Inst_DetectiveCasesProvider caseProvider in s_CaseProviders)
                {
                    caseProvider.ToString().PegiLabel().Write();
                    pegi.ClickHighlight(caseProvider);
                    pegi.Nl();
                    foreach (SO_Detective_Case_Prototype casee in caseProvider.Cases)
                        casee.Enter_Inspect();
                }
        }
        #endregion
    }
}
