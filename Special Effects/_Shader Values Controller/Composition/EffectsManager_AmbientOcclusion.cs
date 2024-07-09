using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public static partial class Effects
    {
        [Serializable]
        public class AmbientOcclusionManager : IPEGI, INeedAttention, IPEGI_ListInspect
        {
            private readonly ShaderProperty.Feature _aoTexture = new("USE_AO_TEXTURE");
            private readonly ShaderProperty.TextureValue _aoTextureGlobal = new("_Global_AO_Lookup");

            [SerializeField] private bool _enableAO = true;
            [SerializeField] private CustomRenderTexture _AOTexture;

            public bool EnableAO
            {
                get => _enableAO;
                set
                {
                    if (_enableAO != value)
                    {
                        _enableAO = value;
                        UpdateShaderGlobal();
                    }
                }
            }

            private void UpdateShaderGlobal()
            {
                _aoTexture.Enabled = _enableAO && _AOTexture;
                _aoTextureGlobal.SetGlobal(_AOTexture);
            }

            public void ManagedUpdate() 
            {
                if (!_enableAO) 
                {
                    return;
                }

            }

            void IPEGI.Inspect()
            {
                var changed = pegi.ChangeTrackStart();

                "AO Tex".PegiLabel(120).Edit(ref _AOTexture).Nl();

                "Not implemented yet. The idea is to blur the depth".PegiLabel().Nl();

                if (_AOTexture)
                {
                    var updMode = _AOTexture.updateMode;

                    if ("Update Mode".PegiLabel().Edit_Enum(ref updMode).Nl())
                        _AOTexture.updateMode = updMode;

                    pegi.Draw(_AOTexture).Nl();
                }

                if (changed)
                    UpdateShaderGlobal();
            }

            public void InspectInList(ref int edited, int index)
            {
                var ao = EnableAO;
                if (pegi.ToggleIcon(ref ao))
                    EnableAO = ao;

                if (ToString().PegiLabel().ClickLabel() | this.Click_Enter_Attention())
                    edited = index;
            }

            public override string ToString() => "AO Texture";

            public string NeedAttention()
            {

                return null;
            }
        }
    }
}
