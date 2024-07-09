using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Modules.SplinePath
{
    [ExecuteAlways]
    [AddComponentMenu(QcUtils.QUIZCANNERS + "/Spline Path/Spline Manager")]
    public class Singleton_SplinePath : Singleton.BehaniourBase, IPEGI, IPEGI_Handles, ICfg
    {

        CfgData data;

        #region Encode & Decode

        protected override void OnAfterEnable()
        {
            base.OnAfterEnable();
            this.Decode(data);
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            base.OnBeforeOnDisableOrEnterPlayMode(afterEnableCalled);
            data = Encode().CfgData;
        }


        public CfgEncoder Encode() => new CfgEncoder().Add("mode", (int)Spline.s_editMode);

        public void DecodeTag(string key, CfgData data)
        {
            switch (key) 
            {
                case "mode": Spline.s_editMode = (Spline.EditMode)data.ToInt(); break;
            }
        }

        #endregion



        #region Inspector

        public void OnSceneDraw()
        {
            foreach (Inst_SplinePath i in Spline.Instances)
                i.OnSceneDraw_Nested();
        }

        public void OnDrawGizmos() => this.OnSceneDraw_Nested();

        private readonly pegi.CollectionInspectorMeta _pathMeta = new("Paths", allowDeleting: false, showAddButton: false, showEditListButton: false);

        public override void Inspect()
        {
            _pathMeta.Edit_List(Spline.Instances).Nl();
        }


        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_SplinePath))] internal class PulseCommanderServiceDrawer : PEGI_Inspector_Override { }
}
