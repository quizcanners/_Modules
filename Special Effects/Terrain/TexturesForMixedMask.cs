using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace QuizCanners.SpecialEffects
{

    [Serializable]
    public class ChannelSetsForDefaultMaps : IPEGI, IGotName, IPEGI_ListInspect
    {
        public const string TERRAIN_SPLAT_DIFFUSE = "_qcPp_mergeSplat_";
        public const string TERRAIN_NORMAL_MAP = "_qcPp_mergeSplatN_";
        public const string TILING = "_qcPp_mergeSplatTiling_";

        public string productName;
        public Texture2D colorTexture;
        public Texture2D normalMap;
        public Texture2D MADS;

        public Texture2D height;
        public Texture2D smooth;
        public Texture2D ambient;
        public Texture2D reflectiveness;

        public bool colorAlphaIsSmoothness;

        void ClearSourceTextures()
        {
            colorTexture = null;
            height = null;
            normalMap = null;
            smooth = null;
            ambient = null;
            reflectiveness = null;
        }


        public Texture2D Product_colorWithAlpha;
        public Texture2D Product_combinedBump;
        public int Size = 1024;
        public float Tiling = 0.1f;
        public float NormalStrength = 1;


        private TextureValue Diffuse;
        private TextureValue Normal;
        private VectorValue TilingValue;

        private readonly Gate.Integer _previousIndex = new();

        internal void UpdateTextures(int index)
        {
            if (_previousIndex.TryChange(index))
            {
                Diffuse = null;
                Normal = null;
                TilingValue = null;
            }


            TilingValue ??= new VectorValue(TILING + index);

            TilingValue.GlobalValue = new Vector4(Tiling, Tiling);

            if (Product_colorWithAlpha)
            {
                Diffuse ??= new TextureValue(TERRAIN_SPLAT_DIFFUSE + index);

                Diffuse.GlobalValue = Product_colorWithAlpha;
            }

            Normal ??= new TextureValue(TERRAIN_NORMAL_MAP + index);

            if (Product_combinedBump)
            {
                Normal.GlobalValue = Product_combinedBump;
            } else 
            {
                Normal.GlobalValue = normalMap;
            }
        }

        internal TerrainLayer UpdateTexturesToLayers(int index)
        {
            UpdateTextures(index);

            return new TerrainLayer()
            {
                diffuseTexture = Product_colorWithAlpha,
                normalMapTexture = Product_combinedBump,
            };
        }



        #region Inspector

        public string NameForInspector
        {
            get { return productName; }
            set { productName = value; }
        }

        public void InspectInList(ref int edited, int ind)
        {
            this.inspect_Name();

            if (Product_colorWithAlpha && !Product_colorWithAlpha.name.Equals(productName))
            {
                if (Icon.Refresh.Click("Set Name"))
                    productName = Product_colorWithAlpha.name;
            }
            else if (colorTexture && !colorTexture.name.Equals(productName) && Icon.Refresh.Click("Set Name"))
                productName = colorTexture.name;

            if (!Product_colorWithAlpha)
                "COl".PegiLabel(40).Edit(ref Product_colorWithAlpha);
            else
                if (!Product_combinedBump)
                "CMB".PegiLabel(40).Edit(ref Product_combinedBump);

            pegi.ClickHighlight(Product_colorWithAlpha);
            pegi.ClickHighlight(Product_combinedBump);

            if (Icon.Enter.Click())
                edited = ind;
        }

        void IPEGI.Inspect()
        {
            if (colorTexture || normalMap || reflectiveness || height || smooth || ambient)
                pegi.Click(ClearSourceTextures).Nl();

            if (Diffuse != null)
                Diffuse.ToString().PegiLabel().Write_ForCopy().Nl();
            else
                (TERRAIN_SPLAT_DIFFUSE + "<index>").PegiLabel().Write_ForCopy().Nl();

            if (Normal != null)
                Normal.ToString().PegiLabel().Write_ForCopy().Nl();
            else
                (TERRAIN_NORMAL_MAP + "<index>").PegiLabel().Write_ForCopy().Nl();


            ClearClick(ref colorTexture);
            "Color".PegiLabel(90).Edit(ref colorTexture).Nl();

            ClearClick(ref normalMap);
            "Bump".PegiLabel(90).Edit(ref normalMap).Nl();

            ClearClick(ref MADS);
            "MADS".PegiLabel(110).Edit(ref MADS).Nl();

            pegi.Line(); 
            pegi.Nl();

            if (colorTexture && !smooth && !reflectiveness)
                "Color Alpha is Gloss".PegiLabel().Toggle(ref colorAlphaIsSmoothness).Nl();

            ClearClick(ref height);
            "Height".PegiLabel(90).Edit(ref height).Nl();
            if (!normalMap && height)
                "Normal from height strength".PegiLabel().Edit(ref NormalStrength, 0, 1f).Nl();

       

            ClearClick(ref smooth);
            "Smooth".PegiLabel(90).Edit(ref smooth).Nl();

            ClearClick(ref ambient);
            "Ambient Occlusion".PegiLabel(90).Edit(ref ambient).Nl();

            ClearClick(ref reflectiveness);
            "Reflectivness".PegiLabel(110).Edit(ref reflectiveness).Nl();

         

            static void ClearClick(ref Texture2D tex)
            {
                if (tex && Icon.Clear.Click()) tex = null;

            }

            "Size".PegiLabel().Edit(ref Size).Nl();

            "Tiling".PegiLabel().Edit(ref Tiling).Nl();

            if (Size < 8)
                "Size is too small".PegiLabel().WriteWarning();
            else
            if (!Mathf.IsPowerOfTwo(Size))
                "Size is not power of two".PegiLabel().WriteWarning();
#if UNITY_EDITOR
            else if ("Generate Mask".PegiLabel().ClickConfirm(confirmationTag: "Recombine Textures"))
            {
                Product_combinedBump = NormalMapFrom(NormalStrength, height, normalMap, ambient, productName, Product_combinedBump);
                if (colorTexture)
                {
                    var gloss = reflectiveness ? reflectiveness : smooth;

                    if (gloss)
                    {
                        Product_colorWithAlpha = MergeRgbAndGloss(gloss: gloss, diffuse: colorTexture, productName);
                    }
                    else if (colorAlphaIsSmoothness)
                    {
                        Product_colorWithAlpha = colorTexture;
                    }
                }

            }
#endif

            pegi.Nl();

            "COLOR+GLOSS".PegiLabel(120).Edit(ref Product_colorWithAlpha).Nl();
            "BUMP+HEIGHT+AO".PegiLabel(120).Edit(ref Product_combinedBump).Nl();
        }




#if UNITY_EDITOR

        private static Color[] _srcBmp;
        private static Color[] _srcSm;
        private static Color[] _srcAmbient;
        private static Color[] _dst;

        private static int _width;
        private static int _height;

        private static int IndexFrom(int x, int y)
        {

            x %= _width;
            if (x < 0) x += _width;
            y %= _height;
            if (y < 0) y += _height;

            return y * _width + x;
        }

        private static Texture2D NormalMapFrom(float strength, Texture2D bump, Texture2D normalReady, Texture2D ambient, string name, Texture2D Result)
        {
            var reference = bump ? bump : normalReady;

            if (!reference)
            {
                Debug.Log("No Base textures");
                return null;
            }

            _width = reference.width;
            _height = reference.height;

            if (bump)
            {
                var importer = bump.GetTextureImporter_Editor();
                var needReimport = importer.WasNotReadable_Editor();
                needReimport |= importer.WasNotSingleChanel_Editor();
                if (needReimport) importer.SaveAndReimport();
            }

            if (normalReady)
            {
                var importer = normalReady.GetTextureImporter_Editor();
                var needReimport = importer.WasNotReadable_Editor();
                needReimport |= importer.WasWrongIsColor_Editor(false);
                needReimport |= importer.WasMarkedAsNormal_Editor();

                if (needReimport)
                    importer.SaveAndReimport();
            }

            if (ambient)
            {
                var importer = ambient.GetTextureImporter_Editor();
                var needReimport = importer.WasNotReadable_Editor()
                 | importer.WasWrongIsColor_Editor(false)
                 | importer.WasNotSingleChanel_Editor();

                if (needReimport)
                    importer.SaveAndReimport();
            }

            try
            {
                _srcBmp = normalReady ? normalReady.GetPixels(_width, _height) : bump.GetPixels();

                _dst = new Color[_height * _width];
            }
            catch (UnityException e)
            {
                Debug.Log("couldn't read one of the textures for  " + bump.name + " " + e);
                return null;
            }


            for (var by = 0; by < _height; by++)
                for (var bx = 0; bx < _width; bx++)
                {
                    var dstIndex = IndexFrom(bx, by);
                    Color destColor = Color.white;

                    if (normalReady)
                    {
                        destColor.r = (_srcBmp[dstIndex].r - 0.5f) * strength + 0.5f;
                        destColor.g = (_srcBmp[dstIndex].g - 0.5f) * strength + 0.5f;
                    }
                    else
                    {
                        var xLeft = _srcBmp[IndexFrom(bx - 1, by)].a;
                        var xRight = _srcBmp[IndexFrom(bx + 1, by)].a;
                        var yUp = _srcBmp[IndexFrom(bx, by - 1)].a;
                        var yDown = _srcBmp[IndexFrom(bx, by + 1)].a;

                        var xDelta = (-xRight + xLeft) * strength;
                        var yDelta = (-yDown + yUp) * strength;

                        destColor.r = xDelta * Mathf.Abs(xDelta) + 0.5f;
                        destColor.g = yDelta * Mathf.Abs(yDelta) + 0.5f;
                    }

                    //destColor.b = _srcSm[dstIndex].a;
                    //destColor.a = _srcAmbient[dstIndex].a;

                    _dst[dstIndex] = destColor;
                }

            if (bump)
            {
                _srcSm = bump.GetPixels(_width, _height);

                for (var by = 0; by < _height; by++)
                    for (var bx = 0; bx < _width; bx++)
                    {
                        var dstIndex = IndexFrom(bx, by);

                        _dst[dstIndex].b = _srcSm[dstIndex].a;
                    }
            }

            if (ambient)
            {
                _srcAmbient = ambient.GetPixels(_width, _height);

                for (var by = 0; by < _height; by++)
                    for (var bx = 0; bx < _width; bx++)
                    {
                        var dstIndex = IndexFrom(bx, by);

                        _dst[dstIndex].a = _srcAmbient[dstIndex].a;
                    }
            }

            if ((!Result) || (Result.width != _width) || (Result.height != _height))
                Result = QcUnity.CreatePngSameDirectory(reference, reference.name + "_QcMask");

            var resImp = Result.GetTextureImporter_Editor();
            if (resImp.WasClamped_Editor()
                 | resImp.WasWrongIsColor_Editor(false)
                 | resImp.WasNotReadable_Editor()
                 | resImp.HadNoMipmaps_Editor())
                resImp.SaveAndReimport();

#pragma warning disable UNT0017 // SetPixels invocation is slow
            Result.SetPixels(_dst);
#pragma warning restore UNT0017 // SetPixels invocation is slow
            Result.Apply();
            QcUnity.SaveChangesToPixels(Result);

            return Result;
        }

        private static Texture2D MergeRgbAndGloss(Texture2D gloss, Texture2D diffuse, string newName)
        {

            if (!gloss)
            {
                Debug.Log("No bump texture");
                return null;
            }

            var ti = gloss.GetTextureImporter_Editor();
            var needReimport = ti.WasNotSingleChanel_Editor();
            needReimport |= ti.WasNotReadable_Editor();
            if (needReimport) ti.SaveAndReimport();


            ti = diffuse.GetTextureImporter_Editor();
            needReimport = ti.WasWrongAlphaIsTransparency_Editor();
            needReimport |= ti.WasNotReadable_Editor();
            if (needReimport) ti.SaveAndReimport();

            var product = QcUnity.CreatePngSameDirectory(diffuse, newName + "_COLOR");

            var importer = product.GetTextureImporter_Editor();
            needReimport = importer.WasNotReadable_Editor();
            needReimport |= importer.WasClamped_Editor();
            needReimport |= importer.HadNoMipmaps_Editor();
            if (needReimport)
                importer.SaveAndReimport();


            _width = gloss.width;
            _height = gloss.height;
            Color32[] dstColor;

            try
            {
                dstColor = diffuse.GetPixels32();
                _srcBmp = gloss.GetPixels(diffuse.width, diffuse.height);
            }
            catch (UnityException e)
            {
                Debug.Log("couldn't read one of the textures for  " + gloss.name + " " + e);
                return null;
            }


            for (var by = 0; by < _height; by++)
            {
                for (var bx = 0; bx < _width; bx++)
                {
                    var dstIndex = IndexFrom(bx, by);
                    var col = dstColor[dstIndex];
                    col.a = (byte)(_srcBmp[dstIndex].a * 255);
                    dstColor[dstIndex] = col;
                }
            }

            product.SetPixels32(dstColor);
            product.Apply();
            QcUnity.SaveChangesToPixels(product);

            return product;
        }
#endif

        #endregion

    }
}
