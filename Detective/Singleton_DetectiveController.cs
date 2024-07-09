using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.DetectiveInvestigations
{

    [ExecuteAlways]
    public class Singleton_DetectiveController : Singleton.BehaniourBase
    {
        #region Inspector

        public override string ToString() => "Detective Cases";

        public override void Inspect()
        {
            base.Inspect();

            Detective.Inspect();
        }

        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_DetectiveController))] internal class Singleton_DetectiveControllerDrawer : PEGI_Inspector_Override { }
}
