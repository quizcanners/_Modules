using UnityEngine;

namespace QuizCanners.Modules.SplinePath
{
    using Inspect;
    using Utils;

    [ExecuteAlways]
    public class Inst_SplinePath : MonoBehaviour, IPEGI, IPEGI_Handles
    {
        [SerializeField] internal Spline.PathType pathType;
        
        [SerializeField] internal SO_SplinePath config;

        internal int Version;

        #region Inspector
        public void OnSceneDraw()
        {
            if (config)
            {
                var changes = pegi.ChangeTrackStart();

                config.OnSceneDraw(transform);

                if (changes)
                {
                    Debug.Log("Setting dirty " + config);
                    config.SetToDirty();
                    Version++;
                }
            }
        }

        void IPEGI.Inspect()
        {
            var changes = pegi.ChangeTrackStart();

            pegi.EditorView.Lock_UnlockClick(this);

            pegi.Nl();

            if (config) 
            {
                "Type".PegiLabel().Edit_Enum(ref pathType).Nl();
                "Edit Mode".PegiLabel().Edit_Enum(ref Spline.s_editMode).Nl();
            }
         
            "Config".PegiLabel(60).Edit(ref config).Nl();

            if (config)
                config.Nested_Inspect();

            if (changes)
                Version++;
        }
        #endregion

        void OnEnable() 
        {
            Spline.Instances.Add(this);
        }

        void OnDisable() 
        {
            Spline.Instances.Remove(this);
        }
    }

    [PEGI_Inspector_Override(typeof(Inst_SplinePath))]
    internal class SplinePath_InstanceDrawer : PEGI_Inspector_Override { }
}