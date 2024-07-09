using QuizCanners.Lerp;
using System.Collections.Generic;
using UnityEngine;
using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace QuizCanners.SpecialEffects
{
    public class Singleton_SceneLighting : Singleton.BehaniourBase, ICfgCustom, IPEGI, ILinkedLerping
    {
        [SerializeField] private SO_SceneLighting_Cfgs _configs;
        [SerializeField] private Light _mainDirectionalLight;

        private readonly LinkedLerp.FloatValue brightness = new("Brightness", 1, 1);
        private readonly LinkedLerp.FloatValue colorBleed = new("Color Bleed", 0, 0.1f);

        private readonly LinkedLerp.ColorValue mainLightColor = new("Light Color");
        private readonly LinkedLerp.FloatValue mainLightIntensity = new(name: "Main Light Intensity");
        private readonly LinkedLerp.QuaternionValue mainLightRotation = new("Main light rotation");

        private readonly LinkedLerp.ColorValue fogColor = new("Fog Color");
        private readonly LinkedLerp.ColorValue skyColor = new("Sky Color");
        private readonly LinkedLerp.FloatValue shadowStrength = new("Shadow Strength", 1);
        private readonly LinkedLerp.FloatValue shadowDistance = new(100, 500, 10, 1000, "Shadow Distance");
        private readonly LinkedLerp.FloatValue fogDistance = new(100, 500, 0.01f, 1000, "Fog Distance");
        private readonly LinkedLerp.FloatValue fogDensity = new(0.01f, 0.01f, 0.00001f, 0.1f, "Fog Density");
        private readonly ShaderProperty.VectorValue _lightProperty = new("pp_COLOR_BLEED");
        private void UpdateShader() => _lightProperty.GlobalValue = new Vector4(colorBleed.CurrentValue, 0, 0, brightness.CurrentValue);

        #region Encode & Decode

        public void ReadCurrentValues()
        {
            fogColor.TargetAndCurrentValue = RenderSettings.fogColor;

            if (RenderSettings.fog)
            {
                fogDistance.TargetAndCurrentValue = RenderSettings.fogEndDistance;
                fogDensity.TargetAndCurrentValue = RenderSettings.fogDensity;
            }

            skyColor.TargetAndCurrentValue = RenderSettings.ambientSkyColor;
            shadowDistance.TargetAndCurrentValue = QualitySettings.shadowDistance;

            if (_mainDirectionalLight)
            {
                mainLightColor.TargetAndCurrentValue = _mainDirectionalLight.color;
                mainLightIntensity.TargetAndCurrentValue = _mainDirectionalLight.intensity;
                mainLightRotation.TargetAndCurrentValue = _mainDirectionalLight.transform.rotation;
            }
        }


        public CfgEncoder Encode()
        {

            if (!enabled)
                ReadCurrentValues();

            var cody = new CfgEncoder()
                    .Add("sh", shadowStrength.TargetValue)
                    .Add("sdst", shadowDistance)
                    .Add("sc", skyColor.targetValue)
                    .Add_Bool("fg", RenderSettings.fog)
                    .Add("lcol", mainLightColor)
                    .Add("lint", mainLightIntensity)
                ;

            cody.Add("lr", mainLightRotation);

            cody.Add("br", brightness);
            cody.Add("bl", colorBleed);

            if (RenderSettings.fog)
                cody.Add("fogCol", fogColor.targetValue)
                    .Add("fogD", fogDistance)
                    .Add("fogDen", fogDensity);

            return cody;
        }

        public void DecodeTag(string tg, CfgData data)
        {
            switch (tg)
            {
                case "sh": shadowStrength.TargetValue = data.ToFloat(); break;
                case "sdst": shadowDistance.Decode(data); break;
                case "sc": skyColor.targetValue = data.ToColor(); break;
                case "fg": RenderSettings.fog = data.ToBool(); break;
                case "fogD": fogDistance.Decode(data); break;
                case "fogDen": fogDensity.Decode(data); break;
                case "fogCol": fogColor.targetValue = data.ToColor(); break;
                case "lr": mainLightRotation.Decode(data); break;
                case "lcol": mainLightColor.Decode(data); break;
                case "lint": mainLightIntensity.Decode(data); break;
                case "br": brightness.Decode(data); break;
                case "bl": colorBleed.Decode(data); break;
            }
        }

        public void DecodeInternal(CfgData data)
        {
            this.Decode(data);
            UpdateShader();
        }

        #endregion

        #region Inspector
        //private int inspectedProperty = -1;
        private readonly pegi.EnterExitContext context = new();
        public static Singleton_SceneLighting inspected;

        public override void Inspect()
        {
            
            inspected = this;


            using (context.StartContext())
            {

                if (Application.isPlaying)
                {
                    if (enabled && "Pause".PegiLabel().Click())
                        enabled = false;

                    if (!enabled && "Control Light".PegiLabel().Click())
                        enabled = true;
                }

                pegi.Nl();

                var changed = pegi.ChangeTrackStart();

             //   bool notInspectingProperty = inspectedProperty == -1;

                "Bleed".PegiLabel(60).Edit(ref colorBleed.targetValue, 0f, 0.3f).Nl();

                "Brightness".PegiLabel(90).Edit(ref brightness.targetValue, 0f, 8f).Nl();

                shadowDistance.Enter_Inspect_AsList().Nl();

                bool fog = RenderSettings.fog;

                if (!context.IsAnyEntered && "Fog".PegiLabel().ToggleIcon(ref fog, true))
                    RenderSettings.fog = fog;

                if (fog)
                {
                    var fogMode = RenderSettings.fogMode;

                    if (!context.IsAnyEntered)
                    {
                        "Fog Color".PegiLabel(60).Edit(ref fogColor.targetValue).Nl();

                        if ("Fog Mode".PegiLabel(60).Edit_Enum(ref fogMode).Nl())
                            RenderSettings.fogMode = fogMode;
                    }

                    if (fogMode == FogMode.Linear)
                        fogDistance.Enter_Inspect_AsList().Nl();
                    else
                        fogDensity.Enter_Inspect_AsList().Nl();
                }

                if (!context.IsAnyEntered)
                    "Sky Color".PegiLabel(60).Edit(ref skyColor.targetValue).Nl();

                pegi.Nl();

                "Main Directional Light".PegiLabel().Edit(ref _mainDirectionalLight).Nl();

                if (_mainDirectionalLight)
                {
                    pegi.Nl();
                    mainLightRotation.Nested_Inspect().Nl();

                    "Light Intensity".PegiLabel().Edit(ref mainLightIntensity.targetValue).Nl();
                    "Light Color".PegiLabel().Edit(ref mainLightColor.targetValue).Nl();
                }

                pegi.Nl();

                if (Application.isPlaying)
                {
                    if (ld.MinPortion < 1)
                    {
                        "Lerping {0}".F(ld.dominantParameter).PegiLabel().Write();
                        pegi.FullWindow.DocumentationClickOpen(() => "Each parameter has a transition speed. THis text shows which parameter sets speed for others (the slowest one). " +
                                                                   "If Transition is too slow, increase this parameter's speed");
                        pegi.Nl();
                    }
                }

                if (changed)
                {
                    Update();
#if UNITY_EDITOR
                    if (Application.isPlaying == false)
                    {
                        pegi.Handle.SceneSetDirty(this);
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    }
#endif
                }

                if (!_configs)
                    pegi.Edit(ref _configs);
                else
                    _configs.Nested_Inspect();

                if (changed)
                    QcUnity.RepaintViews(this);

                if (changed)
                    UpdateShader();
            }

            inspected = null;
        }

        #endregion

        #region Lerping


        private readonly LerpData ld = new(unscaledTime: true);


        private List<LinkedLerp.BaseLerp> lerpsList;


        public void Update()
        {
            ld.Update(this, canSkipLerp: false);
            UpdateShader();
        }

        public void Portion(LerpData ld)
        {

            lerpsList ??= new List<LinkedLerp.BaseLerp>
                    {
                        shadowStrength,
                        shadowDistance,
                        fogColor,
                        skyColor,
                        fogDensity,
                        fogDistance,
                        colorBleed,
                        brightness
                    };

            lerpsList.Portion(ld);

            if (_mainDirectionalLight)
            {
                mainLightIntensity.Portion(ld);
                mainLightColor.Portion(ld);
                mainLightRotation.CurrentValue = _mainDirectionalLight.transform.rotation;
                mainLightRotation.Portion(ld);
            }
        }

        public void Lerp(LerpData ld, bool canSkipLerp)
        {
            lerpsList.Lerp(ld);

            if (_mainDirectionalLight)
            {
                mainLightIntensity.Lerp(ld, canSkipLerp);
                mainLightColor.Lerp(ld, canSkipLerp);
                mainLightRotation.Lerp(ld, canSkipLerp);
            }

            RenderSettings.fogColor = fogColor.CurrentValue;

            if (RenderSettings.fog)
            {
                RenderSettings.fogEndDistance = fogDistance.CurrentValue;
                RenderSettings.fogDensity = fogDensity.CurrentValue;
            }

            RenderSettings.ambientSkyColor = skyColor.CurrentValue;
            QualitySettings.shadowDistance = shadowDistance.CurrentValue;

            if (_mainDirectionalLight)
            {
                _mainDirectionalLight.intensity = mainLightIntensity.CurrentValue;
                _mainDirectionalLight.color = mainLightColor.CurrentValue;
                _mainDirectionalLight.transform.rotation = mainLightRotation.CurrentValue;
            }



        }

        #endregion

    }



    [PEGI_Inspector_Override(typeof(Singleton_SceneLighting))]
internal class PlaytimePainter_SceneLightingManagerInspectorOverride : PEGI_Inspector_Override { }



}
