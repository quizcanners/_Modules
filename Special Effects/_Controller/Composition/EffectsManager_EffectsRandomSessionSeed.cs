using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public partial class Singleton_SpecialEffectShaders
    {
        [System.Serializable]
        public class EffectsRandomSessionSeedManager : IPEGI, IPEGI_ListInspect, IGotReadOnlyName
        {
            [SerializeField] private bool _enabled = true;
            [SerializeField] private float _min = -1;
            [SerializeField] private float _max = 1;
            readonly ShaderProperty.VectorValue RANDOM_SESSION_VALUES = new ShaderProperty.VectorValue("qc_RND_SEEDS");

            public void Regenerate()
            {
                if (_enabled)
                {
                    RANDOM_SESSION_VALUES.GlobalValue = new Vector4(Random.Range(_min, _max), Random.Range(_min, _max), Random.Range(-1, 1), Random.value);
                }
            }

            public void ManagedOnEnable()
            {
                Regenerate();
            }

            public void Inspect()
            {
                var change = pegi.ChangeTrackStart();

                pegi.ToggleIcon(ref _enabled);

                RANDOM_SESSION_VALUES.ToString().PegiLabel().Write_ForCopy(showCopyButton: true).Nl();

                if (_enabled)
                {
                    "Min".PegiLabel(40).Edit(ref _min).Nl();
                    "Max".PegiLabel(40).Edit(ref _max).Nl();

                    "X,Y - Value in selected Range".PegiLabel().WriteHint();
                    "Z - (1- to 1)".PegiLabel().WriteHint();
                    "W - (0 - 1)".PegiLabel().WriteHint();
                }

                if (Icon.Refresh.Click() | change)
                    Regenerate();
            }

            public void InspectInList(ref int edited, int index)
            {
                var changes = pegi.ChangeTrackStart();

                pegi.ToggleIcon(ref _enabled);

                if (GetReadOnlyName().PegiLabel().ClickLabel() | Icon.Enter.Click())
                    edited = index;

                if (Icon.Refresh.Click() | changes)
                    Regenerate();
            }

            public string GetReadOnlyName() => "Random Seed {0}".F(_enabled ? "({0} to {1})".F(_min, _max) : "");
        }
    }
}