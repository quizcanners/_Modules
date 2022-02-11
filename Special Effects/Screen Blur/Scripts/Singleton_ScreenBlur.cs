using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Utils.OnDemandRenderTexture;

namespace QuizCanners.SpecialEffects
{
    [DisallowMultipleComponent]
    public class Singleton_ScreenBlur : Singleton.BehaniourBase, IPEGI
    {
        [SerializeField] protected Camera MyCamera;

        [Header("Management Shaders:")]
        [SerializeField] protected Shader copyShader;
        [SerializeField] protected Shader copyDownscale;

        [Header("Effect Shaders:")]
        [SerializeField] protected Shader blurShader;
        [SerializeField] protected Shader washAwayShader;
        [SerializeField] protected Shader zoomOutShader;

        [Header("Setings:")]
        [SerializeField] private GrabMethod grabMethod = GrabMethod.ScreenCapture;
        private const int MAX_BLUR_FACTOR = 50;
        [SerializeField] protected LogicWrappers.CountUpToMax screenGrabBlurCounter = new LogicWrappers.CountUpToMax(10);
        [SerializeField] private LogicWrappers.CountUpToMax backgroundBlurCounter = new LogicWrappers.CountUpToMax(10);

        private enum GrabMethod { ScreenCapture, RenderFromCamera }

        // Buffer Management
        protected readonly ShaderProperty.TextureValue SCREEN_GRAB_SHD = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Read");
        protected readonly ShaderProperty.TextureValue PROCESSED_SCREEN_SHD = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Effect");
        protected readonly ShaderProperty.TextureValue BACKGROUND_SHD = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Background");
        protected readonly ShaderProperty.FloatValue SCREEN_BLUR_ITERATION = new ShaderProperty.FloatValue("_qcPp_Screen_Blur_Iteration");

        protected readonly ScreenSize BACKGROUND = new ScreenSize("Background Read", useDepth: true);
        protected readonly ScreenSize SCREEN_READ_TEXTURE = new ScreenSize("Screen Read", useDepth: true);
        protected readonly ScreenSize SCREEN_READ_SECOND_BUFFER = new ScreenSize("Second Screen Read Buffer", useDepth: false);
        protected readonly DoubleBuffer EFFECT_DOUBLE_BUFFER = new DoubleBuffer("Blur Effect", isFloat: false);

        protected static MaterialInstancer.ByShader effectMaterialInstance = new MaterialInstancer.ByShader();

        // Request
        [NonSerialized] protected ProcessCommand processCommand;
        [NonSerialized] protected BlurStep step = BlurStep.Off;
        [NonSerialized] protected BackgroundScreenShotData backgroundState = BackgroundScreenShotData.Uninitialized;
        [NonSerialized] protected List<Action> onFirstRenderList = new List<Action>();
        [NonSerialized] protected LogicWrappers.Request backgroundUpdate = new LogicWrappers.Request();


        public void RequestUpdate(Action onFirstRendered = null, ProcessCommand afterScreenGrab = ProcessCommand.Blur, bool updateBackground = true)
        {
            if (onFirstRendered != null)
            {
                onFirstRenderList.Add(onFirstRendered);
            }

            if (step != BlurStep.Off)
            {
                // If any of the commands wanted Blur, use Blur:
                if (afterScreenGrab != ProcessCommand.Nothing)
                    processCommand = afterScreenGrab;
            }
            else
            {
                processCommand = afterScreenGrab;
            }

            if (updateBackground)
                backgroundUpdate.CreateRequest();
            else if (backgroundState == BackgroundScreenShotData.UsingOverlayScreenGrab)
            {
                BlitScreenToBackground();
            }


            step = BlurStep.Requested;
        }

        public void InvalidateBackground()
        {
            backgroundState = BackgroundScreenShotData.Uninitialized;
        }

        private Singleton_ScreenBlurBackgroundController BackgroundGrabber
        {
            get
            {
                var g = Singleton.Get<Singleton_ScreenBlurBackgroundController>();
                return (g && g.IsSetupAndEnabled) ? g : null;
            }
        }

        private void AfterCaptured()
        {
            step = BlurStep.ReturnedFromCamera;
            BlitInternal(SCREEN_READ_TEXTURE, SCREEN_READ_SECOND_BUFFER, copyShader);
            SCREEN_READ_TEXTURE.Version++;
            SCREEN_READ_SECOND_BUFFER.Version++;
        }

        protected void OnPostRender()
        {
            if (step == BlurStep.Requested && grabMethod == GrabMethod.ScreenCapture)
            {
                ScreenCapture.CaptureScreenshotIntoRenderTexture(SCREEN_READ_TEXTURE.GetOrCreateTexture);
                AfterCaptured();
            }
        }

        protected virtual void Update()
        {
            switch (step)
            {
                case BlurStep.Requested:
                    if (grabMethod == GrabMethod.RenderFromCamera)
                    {
                        MyCamera.enabled = false;
                        MyCamera.targetTexture = SCREEN_READ_TEXTURE.GetOrCreateTexture;
                        MyCamera.Render();
                        MyCamera.targetTexture = null;
                        MyCamera.enabled = true;

                        AfterCaptured();
                    }

                    break;
                case BlurStep.ReturnedFromCamera:

                    SCREEN_GRAB_SHD.Set(SCREEN_READ_SECOND_BUFFER);
                    PROCESSED_SCREEN_SHD.Set(SCREEN_READ_SECOND_BUFFER);

                    if (backgroundUpdate.TryUseRequest() || backgroundState == BackgroundScreenShotData.Uninitialized)
                    {
                        if (!BackgroundGrabber)
                        {
                            BACKGROUND_SHD.Set(SCREEN_READ_SECOND_BUFFER);
                            backgroundState = BackgroundScreenShotData.UsingOverlayScreenGrab;
                        }
                        else
                        {
                            RenderBackground();
                        }
                        backgroundBlurCounter.Restart();
                    }

                    if (processCommand == ProcessCommand.Nothing)
                    {
                        if (backgroundBlurCounter.IsFinished == false)
                        {
                            if (backgroundState == BackgroundScreenShotData.UsingOverlayScreenGrab)
                            {
                                //EFFECT_DOUBLE_BUFFER.SetSourceAndTarget(SCREEN_READ_SECOND_BUFFER, BACKGROUND, BACKGROUND_SHD, copyDownscale);
                                Blit(SCREEN_READ_SECOND_BUFFER, BACKGROUND, BACKGROUND_SHD);
                                backgroundState = BackgroundScreenShotData.ExclusiveBuffer;
                            }

                            EFFECT_DOUBLE_BUFFER.SetSourceAndTarget(BACKGROUND, BACKGROUND_SHD, copyDownscale);

                            step = BlurStep.BlurringBackground;
                        }
                        else
                        {
                            step = BlurStep.Off;
                        }
                    }
                    else
                    {
                        EFFECT_DOUBLE_BUFFER.SetSourceAndTarget(SCREEN_READ_SECOND_BUFFER, PROCESSED_SCREEN_SHD, copyDownscale);
                        screenGrabBlurCounter.Restart();
                        step = BlurStep.BlurringFullScreen;
                    }

                    InvokeOnCaptured();

                    break;

                case BlurStep.BlurringFullScreen:

                    screenGrabBlurCounter.AddOne();
                    SCREEN_BLUR_ITERATION.GlobalValue = Math.Min(MAX_BLUR_FACTOR, screenGrabBlurCounter.Count);

                    Shader shade = null;

                    switch (processCommand)
                    {
                        case ProcessCommand.Blur: shade = blurShader; break;
                        case ProcessCommand.WashAway: shade = washAwayShader; break;
                        case ProcessCommand.ZoomOut: shade = zoomOutShader; break;
                        default: QcLog.CaseNotImplemented(processCommand, context: "Get Process Shader"); break;
                    }

                    if (shade)
                    {
                        EFFECT_DOUBLE_BUFFER.BlitToTarget(shade);

                        if (backgroundState == BackgroundScreenShotData.UsingOverlayScreenGrab)
                        {
                            BACKGROUND_SHD.Set(EFFECT_DOUBLE_BUFFER);
                        }
                    }

                    if (screenGrabBlurCounter.IsFinished)
                    {
                        if (!backgroundBlurCounter.IsFinished && backgroundState == BackgroundScreenShotData.ExclusiveBuffer)
                        {
                            Blit(EFFECT_DOUBLE_BUFFER, SCREEN_READ_SECOND_BUFFER, PROCESSED_SCREEN_SHD);
                            EFFECT_DOUBLE_BUFFER.SetSourceAndTarget(BACKGROUND, BACKGROUND_SHD, copyDownscale);
                            step = BlurStep.BlurringBackground;
                        }
                        else
                        {
                            step = BlurStep.Off;
                        }
                    }

                    break;

                case BlurStep.BlurringBackground:

                    backgroundBlurCounter.AddOne();

                    if (backgroundBlurCounter.IsFinished)
                    {
                        step = BlurStep.Off;
                    }
                    else
                    {
                        SCREEN_BLUR_ITERATION.GlobalValue = Math.Min(MAX_BLUR_FACTOR, backgroundBlurCounter.Count);
                        EFFECT_DOUBLE_BUFFER.BlitToTarget(blurShader);
                    }

                    break;
            }


            void InvokeOnCaptured()
            {
                if (onFirstRenderList.Count > 0)
                {
                    var invokes = new List<Action>(onFirstRenderList);
                    onFirstRenderList.Clear();

                    foreach (var act in invokes)
                    {
                        try
                        {
                            act.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
            }

        }

        protected void Blit(ShaderProperty.TextureValue from, ScreenSize to, ShaderProperty.TextureValue tex)
        {
            Graphics.Blit(from.GetGlobal(), to.GetOrCreateTexture, effectMaterialInstance.Get(copyShader));
            tex.GlobalValue = to.GetOrCreateTexture;
        }

        protected void Blit(ScreenSize from, ScreenSize to, ShaderProperty.TextureValue tex)
        {
            Graphics.Blit(from.GetOrCreateTexture, to.GetOrCreateTexture, effectMaterialInstance.Get(copyShader));
            tex.GlobalValue = to.GetOrCreateTexture;
        }

        protected static void Blit(DoubleBuffer from, ScreenSize to, ShaderProperty.TextureValue tex)
        {
            BlitInternal(from, to, Singleton.Get<Singleton_ScreenBlur>().copyShader);
            tex.GlobalValue = to.GetOrCreateTexture;
        }

        private static void BlitInternal(RenderTextureBufferBase from, RenderTextureBufferBase to, Shader shader)
        {
            if (shader)
            {
                Graphics.Blit(from.GetOrCreateTexture, to.GetOrCreateTexture, effectMaterialInstance.Get(shader));
            }
            else
                Graphics.Blit(from.GetOrCreateTexture, to.GetOrCreateTexture);
        }

        void BlitScreenToBackground()
        {
            Blit(PROCESSED_SCREEN_SHD, BACKGROUND, BACKGROUND_SHD);
            BACKGROUND.Version++;
            backgroundState = BackgroundScreenShotData.ExclusiveBuffer;
        }

        void RenderBackground()
        {
            BackgroundGrabber.RenderTo(BACKGROUND.GetOrCreateTexture);
            BACKGROUND.Version++;
            BACKGROUND_SHD.Set(BACKGROUND);
            backgroundState = BackgroundScreenShotData.ExclusiveBuffer;
        }

        #region Inspector
        public override string InspectedCategory => Utils.Singleton.Categories.RENDERING;
        public override void Inspect()
        {
            pegi.Nl();

            if (Application.isPlaying)
            {
                EFFECT_DOUBLE_BUFFER.Nested_Inspect().Nl();
                "Render Blur".PegiLabel().Click().Nl().OnChanged(() => EFFECT_DOUBLE_BUFFER.BlitToTarget(blurShader));
            }

            if ("Settings".PegiLabel().IsFoldout().Nl())
            {
                "Grab Method".PegiLabel(90).EditEnum(ref grabMethod);
                pegi.FullWindow.DocumentationClickOpen(() =>
                {
                    "Screen Grab captures the final result, but is flipped upside-down on some versions of Unity".PegiLabel().Write();
                    return false;
                });

                pegi.Nl();

                "Screen Grab Blur".PegiLabel().Nested_Inspect(ref screenGrabBlurCounter);
                "Background Blur".PegiLabel().Nested_Inspect(ref backgroundBlurCounter);
                pegi.FullWindow.DocumentationClickOpen(() => "For how many frames the Blur operation will be executed.");

                pegi.Nl();

                pegi.FullWindow.DocumentationClickOpen(() => "If you plan to take a screen shot while screen shot is already on the screen, you will to enable this option" +
                                                                "as same texture can't be read from and written to at the same time");
                pegi.Nl();
            }

            if ("Debug".PegiLabel().IsFoldout().Nl())
            {
                if ("Grab Screenshot".PegiLabel().Click())
                    RequestUpdate(afterScreenGrab: ProcessCommand.Nothing, updateBackground: false);

                if ("Grab & Blur".PegiLabel().Click())
                    RequestUpdate(afterScreenGrab: ProcessCommand.Blur, updateBackground: false);

                TestEffect(SCREEN_READ_TEXTURE, SCREEN_GRAB_SHD);
                TestEffect(SCREEN_READ_SECOND_BUFFER, SCREEN_GRAB_SHD);
                TestEffect(BACKGROUND, BACKGROUND_SHD);


                "Background: {0}".F(backgroundState).PegiLabel().Write();

                switch (backgroundState)
                {
                    case BackgroundScreenShotData.UsingOverlayScreenGrab:
                    case BackgroundScreenShotData.ExclusiveBuffer: "Invalidate Background".PegiLabel().Click(InvalidateBackground).Nl(); break;
                    case BackgroundScreenShotData.Uninitialized: "Blit to Background".PegiLabel().Click(BlitScreenToBackground).Nl(); break;
                }

                if (BackgroundGrabber)
                    "Grab".PegiLabel(toolTip: "Grab from Special Camera {0}".F(BackgroundGrabber)).Click().OnChanged(RenderBackground);

                pegi.Nl();

                void TestEffect(OnDemandRenderTexture.ScreenSize buff, ShaderProperty.TextureValue tex)
                {
                    buff.Nested_Inspect();

                    if (tex.GlobalValue != buff.GetIfExists)
                    {
                        if (!tex.GlobalValue)
                        {
                            "{0} texture not set".F(tex.GetNameForInspector()).PegiLabel().Nl();
                        }
                        else
                        {
                            "{0} Is using {1}".F(tex.GetReadOnlyName(), tex.GlobalValue.name).PegiLabel().Nl();
                            // tex.GlobalValue.draw("textire", 256).nl();
                        }
                    }

                    pegi.Nl();
                    "To Effect Buffer".PegiLabel().Click().Nl().OnChanged(() => EFFECT_DOUBLE_BUFFER.SetSourceAndTarget(buff, tex, copyDownscale));
                }

                pegi.Nl();
            }


            if (MyCamera && MyCamera.allowHDR)
            {

                "Allow HDR often causes flip of the image".PegiLabel().WriteWarning().Nl();
            }

        }
        #endregion

        void Reset()
        {
            MyCamera = GetComponent<Camera>();
            step = BlurStep.Off;
        }

        protected enum BackgroundScreenShotData { Uninitialized, ExclusiveBuffer, UsingOverlayScreenGrab }

        public enum ProcessCommand
        {
            Blur, Nothing, WashAway, ZoomOut
        }

        protected enum BlurStep
        {
            Off,
            Requested,
            ReturnedFromCamera,
            BlurringFullScreen,
            BlurringBackground
        }

        /*
    protected static void BlitGL(Texture source, RenderTexture destination, Material mat)
    {
        RenderTexture.active = destination;
        mat.SetTexture("_MainTex", source);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.invertCulling = true;
        mat.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 0.0f);
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 0.0f);
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f);
        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);
        GL.End();
        GL.invertCulling = false;
        GL.PopMatrix();
    }*/
    }


    [PEGI_Inspector_Override(typeof(Singleton_ScreenBlur))] internal class ScreenBlurControllerDrawer : PEGI_Inspector_Override { }
}