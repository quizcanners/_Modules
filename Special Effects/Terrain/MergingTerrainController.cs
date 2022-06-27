using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace QuizCanners.SpecialEffects
{
    [ExecuteAlways]
    public class MergingTerrainController : MonoBehaviour, IPEGI
    {
        public List<ChannelSetsForDefaultMaps> mergeSubMasks;
        public Terrain terrain;
        public Texture2D terrainHeightTexture;

        [SerializeField] private Vector4 _terrainScale;
        [SerializeField] private Vector4 _terrainTiling;
        public float tilingY = 8;

        public const string TERRAIN_CONTROL_TEXTURE = "_qcPp_mergeControl";

        public static readonly VectorValue TerrainPosition = new("_qcPp_mergeTeraPosition");
        public static readonly VectorValue TerrainTiling = new("_qcPp_mergeTerrainTiling");
        public static readonly VectorValue TerrainScale = new("_qcPp_mergeTerrainScale");
        public static readonly TextureValue TerrainHeight = new("_qcPp_mergeTerrainHeight");
        public static readonly TextureValue TerrainControlMain = new(TERRAIN_CONTROL_TEXTURE);


        private void OnEnable()
        {
            UpdateShaderValues();
            UpdateSplatTextures();
        }

        private void UnityTerrain_To_HeightTexture()
        {

            var td = terrain.terrainData;

            var textureSize = td.heightmapResolution - 1;

            if (!terrainHeightTexture || terrainHeightTexture.width != textureSize)
            {
                Debug.Log("Wrong size: {0} textureSize {1}".F(terrainHeightTexture.width, terrainHeightTexture.width));

                return;
            }

            var col = terrainHeightTexture.GetPixels();

            var height = 1f / td.size.y;

            for (var y = 0; y < textureSize; y++)
            {
                var fromY = y * textureSize;

                for (var x = 0; x < textureSize; x++)
                {
                    var tmpCol = new Color();

                    var dx = ((float)x) / textureSize;
                    var dy = ((float)y) / textureSize;

                    var v3 = td.GetInterpolatedNormal(dx, dy); // + Vector3.one * 0.5f;

                    tmpCol.r = v3.x + 0.5f;
                    tmpCol.g = v3.y + 0.5f;
                    tmpCol.b = v3.z + 0.5f;
                    tmpCol.a = td.GetHeight(x, y) * height;

                    col[fromY + x] = tmpCol;
                }
            }

#pragma warning disable UNT0017 // SetPixels Needed for Floating point calculations
            terrainHeightTexture.SetPixels(col);
#pragma warning restore UNT0017 // SetPixels invocation is slow

            terrainHeightTexture.Apply(true, false);

            QcUnity.TrySaveTexture(ref terrainHeightTexture);

        }

        private void TerrainHeightTexture_To_UnityTerrain()
        {
            var td = terrain.terrainData;

            var res = td.heightmapResolution - 1;

            var conversion = (terrainHeightTexture.width / (float)res);

            var heights = td.GetHeights(0, 0, res + 1, res + 1);

            var cols = terrainHeightTexture.GetPixels();

            if (Math.Abs(conversion - 1) > float.Epsilon)
                for (var y = 0; y < res; y++)
                {
                    var yInd = terrainHeightTexture.width * Mathf.FloorToInt((y * conversion));
                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yInd + (int)(x * conversion)].a;

                }
            else
                for (var y = 0; y < res; y++)
                {
                    var yInd = terrainHeightTexture.width * y;

                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yInd + x].a;
                }

            for (var y = 0; y < res - 1; y++)
                heights[y, res] = heights[y, res - 1];
            for (var x = 0; x < res; x++)
                heights[res, x] = heights[res - 1, x];

            terrain.terrainData.SetHeights(0, 0, heights);
        }

        private void UpdateSplatTextures()
        {
         

            if (mergeSubMasks.IsNullOrEmpty())
                return;

            TerrainLayer[] prots = new TerrainLayer[mergeSubMasks.Count]; // terrain.terrainData.terrainLayers;

            for (var i = 0; i < mergeSubMasks.Count; i++)
            {
                prots[i] = mergeSubMasks[i].UpdateSplatTextures(i);
            }


            if (!terrain)
                terrain = GetComponent<Terrain>();

            if (!terrain)
                return;

            if (terrain.terrainData != null)
            {
                terrain.terrainData.terrainLayers = prots;
                terrain.terrainData.SetToDirty();
            }
        }

        private void UpdateShaderValues()
        {

            if (terrain && terrain.terrainData)
            {
                var sp = terrain.terrainData.terrainLayers;

                var td = terrain.terrainData;
                var tds = td.size;

                if (sp.Length != 0 && sp[0] != null)
                {
                    var tilingX = tds.x / sp[0].tileSize.x;
                    var tilingZ = tds.z / sp[0].tileSize.y;

                    _terrainTiling = new Vector4(tilingX, tilingZ, sp[0].tileOffset.x, sp[0].tileOffset.y);

                  

                    tilingY = td.size.y / sp[0].tileSize.x;
                }

                _terrainScale = new Vector4(tds.x, tds.y, tds.z, 0.5f / td.heightmapResolution);

                var alphaMapTextures = td.alphamapTextures;
                if (!alphaMapTextures.IsNullOrEmpty())
                    TerrainControlMain.GlobalValue = alphaMapTextures[0];
            }

            TerrainScale.GlobalValue = _terrainScale;
            TerrainTiling.GlobalValue = _terrainTiling;
            TerrainPosition.GlobalValue = transform.position.ToVector4(tilingY);
            TerrainHeight.GlobalValue = terrainHeightTexture;
        }

        private void CreateTerrainHeightTexture()
        {

          //  TextureValue field = TerrainHeight;

            var size = terrain.terrainData.heightmapResolution - 1;

            terrainHeightTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            terrainHeightTexture.name = gameObject.scene.name + "-" + TerrainHeight.ToString();
            terrainHeightTexture.Apply(true, false);

            UnityTerrain_To_HeightTexture();

            terrainHeightTexture.wrapMode = TextureWrapMode.Clamp;

#if UNITY_EDITOR

            string name = terrainHeightTexture.name;
            terrainHeightTexture = QcUnity.SaveTextureAsAsset(terrainHeightTexture, "Textures", ref name, saveAsNew: false);
            terrainHeightTexture.Reimport_IfNotReadale_Editor();

            var importer = terrainHeightTexture.GetTextureImporter_Editor();
            var needReimport = importer.WasNotReadable_Editor();
            needReimport |= importer.WasWrongIsColor_Editor(false);
            if (needReimport) importer.SaveAndReimport();
#endif
        }


        private readonly Gate.Vector3Value _position = new();


        void Update() 
        {
            if (terrainHeightTexture && _position.TryChange(transform.position)) 
            {
                UpdateShaderValues();
            }
        }

        #region Inspector

        private readonly pegi.CollectionInspectorMeta _collectionMeta = new pegi.CollectionInspectorMeta("Merge Sub Masks");
        private readonly pegi.EnterExitContext _context = new pegi.EnterExitContext();

        public void Inspect()
        {
            using (_context.StartContext())
            {
                pegi.Nl();

                if (_context.IsAnyEntered == false)
                {
                    var changed = pegi.ChangeTrackStart();

                    "Terrain".PegiLabel().Edit_IfNull(ref terrain, gameObject);

                    if (!terrain && Icon.NewTexture.Click())
                        CreateTerrainHeightTexture();

                    pegi.Nl();

                    "Height Texture".PegiLabel(70).Edit(ref terrainHeightTexture).Nl();

                 

                    if (!terrainHeightTexture)
                    {
                        pegi.Click(CreateTerrainHeightTexture);
                    }
                    else
                    {
                        if ("Tex2D To Terrain".PegiLabel().ClickConfirm(confirmationTag: "Load Terrain From Texture").Nl())
                            TerrainHeightTexture_To_UnityTerrain();

                        if ("Terrain To Tex2D".PegiLabel().Click().Nl())
                            UnityTerrain_To_HeightTexture();

                        if (changed | "Update Global Values".PegiLabel().Click().Nl())
                            UpdateShaderValues();

                        if (terrain)
                        {
                            var mat = terrain.materialTemplate;
                            if ("Material".PegiLabel(60).Edit(ref mat).Nl())
                                terrain.materialTemplate = mat;
                        }
                    }
                }
                pegi.Nl();


                if ("Splat".PegiLabel().IsEntered().Nl())
                {
                    var changed = pegi.ChangeTrackStart();

                    _collectionMeta.Edit_List(mergeSubMasks).Nl(UpdateSplatTextures);

                    if (changed | "Update".PegiLabel().Click())
                    {
                        UpdateSplatTextures();
                    }
                }

            }
        }


        #endregion




        [Serializable]
        public class ChannelSetsForDefaultMaps : IPEGI, IGotName, IPEGI_ListInspect
        {
            public const string TERRAIN_SPLAT_DIFFUSE = "_qcPp_mergeSplat_";
            public const string TERRAIN_NORMAL_MAP = "_qcPp_mergeSplatN_";
            public const string TILING = "_qcPp_mergeSplatTiling_";

            public string productName;
            public Texture2D colorTexture;
            public Texture2D height;
            public Texture2D normalMap;
            public Texture2D smooth;
            public Texture2D ambient;
            public Texture2D reflectiveness;

            public Texture2D Product_colorWithAlpha;
            public Texture2D Product_combinedBump;
            public int Size = 1024;
            public float Tiling = 0.1f;
            public float NormalStrength = 1;

            private TextureValue Diffuse;
            private TextureValue Normal;
            private VectorValue TilingValue;

            internal TerrainLayer UpdateSplatTextures(int index) 
            {
                if (TilingValue == null)
                    TilingValue = new VectorValue(TILING + index);

                TilingValue.GlobalValue = new Vector4(Tiling, Tiling);

                if (Product_colorWithAlpha)
                {
                    if (Diffuse == null)
                        Diffuse = new TextureValue(TERRAIN_SPLAT_DIFFUSE + index);

                    Diffuse.GlobalValue = Product_colorWithAlpha;
                }

                if (Product_combinedBump)
                {
                    if (Normal == null)
                        Normal = new TextureValue(TERRAIN_NORMAL_MAP + index);

                    Normal.GlobalValue = Product_combinedBump;
                }

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
                } else if (colorTexture && !colorTexture.name.Equals(productName) && Icon.Refresh.Click("Set Name"))
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

            public void Inspect()
            {
                if (Diffuse != null)
                    Diffuse.ToString().PegiLabel().Write_ForCopy().Nl();
                else
                    (TERRAIN_SPLAT_DIFFUSE + "<index>").PegiLabel().Write_ForCopy().Nl();

                if (Normal != null)
                    Normal.ToString().PegiLabel().Write_ForCopy().Nl();
                else
                    (TERRAIN_NORMAL_MAP + "<index>").PegiLabel().Write_ForCopy().Nl();

                "Color".PegiLabel(90).Edit(ref colorTexture).Nl();
                "Height".PegiLabel(90).Edit(ref height).Nl();
                if (!normalMap && height)
                    "Normal from height strength".PegiLabel().Edit(ref NormalStrength, 0, 1f).Nl();
                "Bump".PegiLabel(90).Edit(ref normalMap).Nl();

                "Smooth".PegiLabel(90).Edit(ref smooth).Nl();
                "Ambient Occlusion".PegiLabel(90).Edit(ref ambient).Nl();
                "Reflectivness".PegiLabel(110).Edit(ref reflectiveness).Nl();

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
                   // if (colorTexture != null)
                     //   Product_colorWithAlpha = GlossToAlpha(smooth, colorTexture, productName);

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
                    Result = QcUnity.CreatePngSameDirectory(reference, name + "_QcMask");

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

            private static Texture2D GlossToAlpha(Texture2D gloss, Texture2D diffuse, string newName)
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


    [PEGI_Inspector_Override(typeof(MergingTerrainController))] internal class MergingTerrainEditor : PEGI_Inspector_Override { }

}