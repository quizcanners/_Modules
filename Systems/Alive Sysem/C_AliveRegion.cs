using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.AliveWorld
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu(Alive.ADD_COMPONENT_MENU + "/Alive Region")]
    public class C_AliveRegion : MonoBehaviour, IPEGI, IPEGI_Handles
    {
        public Alive.Region.Type Type;
        public int Vacancy = 4;
        

        public Vector3 BoundsCenter
        {
            get
            {
                var tf = transform;
                return tf.position + 0.5f * tf.localScale.y * Vector3.up;
            }
            set
            {
                var tf = transform;
                tf.position = value - 0.5f * tf.localScale.y * Vector3.up;
            }
        }

        public Vector3 Size => transform.localScale;

        void OnEnable() 
        {
            Alive.s_regions.Add(this);
        }

        void OnDisable() 
        {
            Alive.s_regions.Remove(this);
        }

        #region Inspector

        public override string ToString() => "{0}: {1}".F(Type.ToString(), gameObject.name);

        void IPEGI.Inspect()
        {

            "Type".PegiLabel(40).Edit_Enum(ref Type).Nl();


            pegi.Nl();
        }

        public void OnSceneDraw()
        {
            Vector3 pos = BoundsCenter;
            Vector3 size = Size;
            if (pegi.Handle.BoxBoundsHandle(ref pos, ref size, Color.green)) 
            {
                transform.localScale = size;
                BoundsCenter = pos;
            }
        }

        #endregion
    }

    [PEGI_Inspector_Override(typeof(C_AliveRegion))] internal class C_AliveRegionDrawer : PEGI_Inspector_Override { }
}
