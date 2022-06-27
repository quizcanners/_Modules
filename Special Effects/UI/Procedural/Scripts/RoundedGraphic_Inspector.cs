using static QuizCanners.Inspect.pegi;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;
using QuizCanners.Inspect;

namespace QuizCanners.SpecialEffects
{
    public partial class RoundedGraphic : IPEGI
    {

        private static List<Shader> _compatibleShaders;

        private static List<Shader> CompatibleShaders
        {
            get
            {
                if (_compatibleShaders == null)
                {
                    _compatibleShaders = new List<Shader>()
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Lit Button"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Box"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Unlinked/Box Unlinked"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Pixel Perfect"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Outline"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Unlinked/Outline Unlinked"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Button With Shadow"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Shadow"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Glow"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Gradient"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Unlinked/Gradient Unlinked"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Preserve Aspect"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/SubtractiveGraphic"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Image"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Pixel Line"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Pixel Perfect Screen Space"))
                        .TryAdd(Shader.Find("Quiz cAnners/UI/Procedural/Box Screen Blur"));
                }

                return _compatibleShaders;
            }
        }

        private static List<Material> _compatibleMaterials = new List<Material>();

        public static RoundedGraphic inspected;

        private const string info =
            "Rounded Graphic component provides additional data to pixel perfect UI shaders. Those shaders will often not display correctly in the scene view. " +
            "Also they may be tricky at times so take note of all the warnings and hints that my show in this inspector. " +
            "When Canvas is set To ScreenSpace-Camera it will also provide adjustive softening when scaled";

        internal static void ClickDuplicate(ref Material mat, string newName = null, string folder = "Materials") =>
            ClickDuplicate(ref mat, folder, ".mat", newName);

        internal static void ClickDuplicate<T>(ref T obj, string folder, string extension, string newName = null) where T : Object
        {
            if (!obj) 
                return;

            if (Application.isEditor)
            {
                #if UNITY_EDITOR
                var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
                if (Icon.Copy.ClickConfirm("dpl" + obj + "|" + path, "{0} Duplicate at {1}".F(obj, path)))
                {
                    obj = QcUnity.Duplicate(obj, folder, extension: extension, newName: newName);
                }
                #endif
            }
            else 
            {
                if (Icon.Copy.Click("Create Instance of {0}".F(obj)))
                    obj = Instantiate(obj);
            }
        }

        private readonly CollectionInspectorMeta _modulesInspectorMeta = new CollectionInspectorMeta("Modules");

        [SerializeField] private EnterExitContext _enteredContent = new EnterExitContext();

        public void Inspect()
        {
            using (_enteredContent.StartContext())
            {
                inspected = this;

                FullWindow.DocumentationClickOpen(info, "About Rounded Graphic").Nl();

                var mat = material;

                var can = canvas;

                var shad = mat.shader;

                var changed = ChangeTrackStart();

                bool expectedScreenPosition = false;

                bool expectedAtlasedPosition = false;

                if (!_enteredContent.IsAnyEntered)
                {

                    bool gotPixPerfTag = false;

                    bool mayBeDefaultMaterial = true;

                    bool expectingPosition = false;

                    bool possiblePositionData = false;

                    bool possibleFadePosition = false;

                    bool needThirdUv;

                    #region Material Tags 
                    if (mat)
                    {
                        var pixPfTag = mat.Get(ShaderTags.PixelPerfectUi);

                        gotPixPerfTag = !pixPfTag.IsNullOrEmpty();

                        if (!gotPixPerfTag)
                            "{0} doesn't have {1} tag".F(shad.name, ShaderTags.PixelPerfectUi.ToString()).PegiLabel().WriteWarning();
                        else
                        {

                            mayBeDefaultMaterial = false;

                            expectedScreenPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.Position.ToString());

                            if (!expectedScreenPosition)
                            {

                                expectedAtlasedPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.AtlasedPosition.ToString());

                                if (!expectedAtlasedPosition)
                                    possibleFadePosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.FadePosition.ToString());
                            }

                            needThirdUv = expectedAtlasedPosition || (possibleFadePosition && feedPositionData);

                            expectingPosition = expectedAtlasedPosition || expectedScreenPosition;

                            possiblePositionData = expectingPosition || possibleFadePosition;

                            if (!can)
                                "No Canvas".PegiLabel().WriteWarning();
                            else
                            {
                                if ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                                {

                                    "Material requires Canvas to pass Edges data trough Texture Coordinate 1 data channel".PegiLabel()
                                        .WriteWarning();
                                    if ("Fix Canvas Texture Coordinate 1".PegiLabel().Click().Nl())
                                        can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

                                }

                                if (possiblePositionData && feedPositionData)
                                {
                                    if ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord2) == 0)
                                    {
                                        "Material requires Canvas to pass Position Data trough Texcoord2 channel".PegiLabel()
                                            .WriteWarning();
                                        if ("Fix Canvas ".PegiLabel().Click().Nl())
                                            can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                                    }
                                    else if (needThirdUv && (can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord3) == 0)
                                    {

                                        "Material requires Canvas to pass Texoord3 channel".PegiLabel().WriteWarning();
                                        if ("Fix Canvas".PegiLabel().Click().Nl())
                                            can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord3;
                                    }

                                }

                                if (can.renderMode == RenderMode.WorldSpace)
                                {
                                    "Rounded UI isn't always working on world space UI yet.".PegiLabel().WriteWarning();
                                   // if ("Change to Overlay".PegiLabel().Click())
                                        //can.renderMode = RenderMode.ScreenSpaceOverlay;
                                    if ("Change to Camera".PegiLabel().Click())
                                        can.renderMode = RenderMode.ScreenSpaceCamera;
                                    Nl();
                                }

                            }
                        }
                    }
                    #endregion

                    var linked = LinkedCorners;

                    if (mat && (linked == mat.IsKeywordEnabled(UNLINKED_VERTICES)))
                        mat.SetShaderKeyword(UNLINKED_VERTICES, !linked);

                    if (Toggle(ref linked, Icon.Link, Icon.UnLinked))
                        LinkedCorners = linked;

                    for (var i = 0; i < _roundedCournersPixels.Length; i++)
                    {
                        var crn = _roundedCournersPixels[i];

                        if ("{0}".F(linked ? "Courners" : ((Corner)i).ToString()).PegiLabel(70).Edit(ref crn, 0, MaxCourner).Nl())
                            _roundedCournersPixels[i] = crn;
                    }

                    Nl();

                    if (mat)
                    {
                        var needLink = ShaderTags.PerEdgeData.Get(mat);
                        if (!needLink.IsNullOrEmpty())
                        {
                            if (ShaderTags.PerEdgeRoles.LinkedCourners.Equals(needLink))
                            {
                                if (!linked)
                                {
                                    "Material expects edge data to be linked".PegiLabel().WriteWarning();
                                    if ("FIX".PegiLabel().Click())
                                        LinkedCorners = true;
                                }
                            }
                            else
                            {
                                if (linked)
                                {
                                    "Material expects edge data to be Unlinked".PegiLabel().WriteWarning();
                                    if ("FIX".PegiLabel().Click())
                                        LinkedCorners = false;
                                }
                            }
                        }
                    }

                    Nl();

                    QcUnity.RemoveEmpty(_compatibleMaterials);

                    if (mat && gotPixPerfTag)
                        _compatibleMaterials.AddIfNew(mat);

                    bool showingSelection = false;

                    var cmpCnt = _compatibleMaterials.Count;
                    if (cmpCnt > 0 && ((cmpCnt > 1) || (!_compatibleMaterials[0].Equals(mat))))
                    {

                        showingSelection = true;

                        if (Select(ref mat, _compatibleMaterials, allowInsert: !mayBeDefaultMaterial))
                            material = mat;
                    }

                    if (mat)
                    {

                        if (!Application.isPlaying)
                        {
                            var path = QcUnity.GetAssetFolder(mat);
                            if (path.IsNullOrEmpty())
                            {
                                Nl();
                                "Material is not saved as asset. Click COPY next to it to save as asset. Or Click 'Refresh' to find compatible materials in your assets ".PegiLabel().Write_Hint();
                                Nl();
                            }
                            else
                                mayBeDefaultMaterial = false;
                        }

                        if (!showingSelection && !mayBeDefaultMaterial)
                        {
                            var n = mat.name;
                            if ("Rename Material".PegiLabel("Press Enter to finish renaming.", 120).Edit_Delayed(ref n))
                                QcUnity.RenameAsset(mat, n);
                        }
                    }

                    ChangesToken changedMaterial = Edit_Property(() => m_Material, this, fieldWidth: 60);

                    if (!Application.isPlaying)
                        changedMaterial |= Nested_Inspect(() => ClickDuplicate(ref mat, gameObject.name)).OnChanged(() => material = mat);

                    if (changedMaterial)
                        _compatibleMaterials.AddIfNew(material);

                    if (!Application.isPlaying && Icon.Refresh.Click("Find All Compatible Materials in Assets"))
                        _compatibleMaterials = ShaderTags.PixelPerfectUi.GetTaggedMaterialsFromAssets();

                    Nl();

                    if (mat && !mayBeDefaultMaterial)
                    {

                        if ("Shader".PegiLabel(60).Select(ref shad, CompatibleShaders, false, true))
                            mat.shader = shad;

                        var sTip = mat.Get(QuizCanners.Utils.ShaderTags.ShaderTip);

                        if (!sTip.IsNullOrEmpty())
                            FullWindow.DocumentationClickOpen(sTip, "Tip from shader tag");

                        if (shad)
                            ClickHighlight(shad);

                        if (Icon.Refresh.Click("Refresh compatible Shaders list"))
                            _compatibleShaders = null;
                    }

                    Nl();

                    "Color".PegiLabel(90).Edit_Property(() => color, this).Nl();

                    #region Position Data

                    if (possiblePositionData || feedPositionData)
                    {

                        "Position Data".PegiLabel().ToggleIcon(ref feedPositionData, true);

                        if (feedPositionData)
                        {
                            "Position: ".PegiLabel(60).Edit_Enum(ref _positionDataType);

                            FullWindow.DocumentationClickOpen("Shaders that use position data often don't look right in the scene view.", "Camera dependancy warning");

                            Nl();
                        }
                        else if (expectingPosition)
                            "Shader expects Position data".PegiLabel().WriteWarning();

                        if (gotPixPerfTag)
                        {

                            if (feedPositionData)
                            {

                                switch (_positionDataType)
                                {
                                    case PositionDataType.ScreenPosition:

                                        if (expectedAtlasedPosition)
                                            "Shader is expecting Atlased Position".PegiLabel().WriteWarning();

                                        break;
                                    case PositionDataType.AtlasPosition:
                                        if (expectedScreenPosition)
                                            "Shader is expecting Screen Position".PegiLabel().WriteWarning();
                                        else if (sprite && sprite.packed)
                                        {
                                            if (sprite.packingMode == SpritePackingMode.Tight)
                                                "Tight Packing is not supported by rounded UI".PegiLabel().WriteWarning();
                                            else if (sprite.packingRotation != SpritePackingRotation.None)
                                                "Packing rotation is not supported by Rounded UI".PegiLabel().WriteWarning();
                                        }

                                        break;
                                    case PositionDataType.FadeOutPosition:

                                        "Fade out at".PegiLabel().Edit(ref fadeOutUvPosition).Nl();

                                        break;
                                }
                            }
                        }

                        Nl();
                    }

                    if (gotPixPerfTag && feedPositionData && !possiblePositionData)
                        "Shader doesn't have any PixelPerfectUI Position Tags. Position updates may not be needed".PegiLabel().WriteWarning();

                    Nl();

                    #endregion

                    var spriteTag = mat ? mat.Get(ShaderTags.SpriteRole) : null;

                    var noTag = spriteTag.IsNullOrEmpty();

                    if (noTag || !spriteTag.SameAs(ShaderTags.SpriteRoles.Hide.ToString()))
                    {
                        if (noTag)
                            spriteTag = "Sprite";

                        spriteTag.PegiLabel(90).Edit_Property(() => sprite, this).Nl();

                        var sp = sprite;

                        if (sp)
                        {
                            var tex = sp.texture;

                            var rct = SpriteRect;

                            if (tex && (
                                !Mathf.Approximately(rct.width, rectTransform.rect.width)
                                || !Mathf.Approximately(rct.height, rectTransform.rect.height))
                                    && Icon.Size.Click("Set Native Size").Nl())
                            {
                                rectTransform.sizeDelta = SpriteRect.size;
                                this.SetToDirty();
                            }
                        }
                        Nl();
                    }

                    "Maskable".PegiLabel(90).Edit_Property(() => maskable, this, includeChildren: true).Nl();

                    "Raycast Target".PegiLabel(90).Edit_Property(() => raycastTarget, this).Nl();
                }

                if (_modulesInspectorMeta.Enter_List(_modules).Nl())
                {
                    ConfigStd = Encode().CfgData;
                    this.SetToDirty();
                }

                if (changed)
                {
                    SetVerticesDirty();
                }
            }
        }
    }


    [PEGI_Inspector_Override(typeof(RoundedGraphic))] internal class PixelPerfectShaderDrawer : PEGI_Inspector_Override { }
}
