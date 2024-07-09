using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEditor;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace QuizCanners.SpecialEffects
{
    [ExecuteAlways]
    public class MergingTerrainController : MonoBehaviour, IPEGI, INeedAttention
    {
        public SO_MergingTerrainLayers mergeSubMasks;
        public Terrain terrain;
        public Texture2D terrainHeightTexture;
        public Texture2D terrainControlTexture;

        [SerializeField] private Vector4 _terrainScale;
        [SerializeField] private Vector4 _terrainTiling;
        public float tilingY = 8;

        public const string TERRAIN_CONTROL_TEXTURE = "_qcPp_mergeControl";

        public static readonly VectorValue TerrainPosition = new("_qcPp_mergeTeraPosition");
        public static readonly VectorValue TerrainTiling = new("_qcPp_mergeTerrainTiling");
        public static readonly VectorValue TerrainScale = new("_qcPp_mergeTerrainScale");
        public static readonly TextureValue TerrainHeight = new("_qcPp_mergeTerrainHeight");
        public static readonly TextureValue TerrainControlMain = new(TERRAIN_CONTROL_TEXTURE);
        public static readonly Feature MERGING_TERRAIN_ON = new ("QC_MERGING_TERRAIN");


        private readonly TexAndProp AlbedoArray = new("Qc_TerrainAlbedos", isColor: true);
        private readonly TexAndProp BumpMapsArray = new("Qc_TerrainMaps", isColor: false);
        private readonly TexAndProp MADS_Array = new("Qc_TerrainMADS", isColor: false);
        private readonly VectorArrayValue SPLAT_TILING = new("Qc_mergeSplatTiling");

        private class TexAndProp : IPEGI
        {
            private string _name;
            private RenderTexture array;
            private TextureValue property; // = new("Qc_TerrainAlbedos");
            private bool _isColor;
            private readonly LogicWrappers.Request _mipsDirty = new();

            const int MAPS_COUNT = 5;

            public TextureValue GetPRoperty()=> property ??= new TextureValue(_name);

            public void Clear() 
            {
                if (array)
                {
                    array.DestroyWhatever();
                    array = null;
                }
            }

            void GetTextureArray(out RenderTexture tex)
            {
                if (array == null)
                {
                    array = new RenderTexture(width: 1024, 1024, depth: 0, RenderTextureFormat.ARGB32)
                    {
                        useMipMap = true,
                        autoGenerateMips = false,
                        dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
                        volumeDepth = MAPS_COUNT,
                        name = _name,
                        wrapMode = TextureWrapMode.Repeat, 
                    };

                }

                tex = array;
            }

            public void SetTextureArray()
            {
                if (array)
                {
                    if (_mipsDirty.TryUseRequest())
                    {
                        array.GenerateMips();
                    }

                    GetPRoperty().SetGlobal(array);
                }
            }

            public void Set(Texture tex, int index)
            {
                GetTextureArray(out RenderTexture target);
                Graphics.Blit(tex, target, sourceDepthSlice: 0, destDepthSlice: index);
                _mipsDirty.CreateRequest();
                
            }


            public override string ToString() => "Tex {0}[{1}]".F(_name, MAPS_COUNT);

            void IPEGI.Inspect()
            {
                ToString().PegiLabel(pegi.Styles.BaldText).Nl();
                "Tex".PegiLabel().Edit(ref array).Nl();


            }

            public TexAndProp (string name, bool isColor) 
            {
                _name = name;
                _isColor = isColor;
            }

        }

        private void OnEnable()
        {
            UpdateSplatTextures(updateOnTerrainComponent: false);
            UpdateShaderValues();
            MERGING_TERRAIN_ON.Enabled = true;
        }

        private void OnDisable()
        {
            MERGING_TERRAIN_ON.Enabled = false;
            AlbedoArray.Clear();
            BumpMapsArray.Clear();
            MADS_Array.Clear();
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

                    var v3 = td.GetInterpolatedNormal(dx, dy) * 0.5f; // + Vector3.one * 0.5f;

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

        private void UpdateSplatTextures(bool updateOnTerrainComponent = false)
        {
            if (!mergeSubMasks)
                return;

            var masks = mergeSubMasks.mergeSubMasks;

            if (!updateOnTerrainComponent)
            {
                Vector4[] tiling = new Vector4[5];

                for (var i = 0; i < masks.Count; i++)
                {
                    var m = masks[i];
                    m.UpdateTextures(i);

                    AlbedoArray.Set(m.colorTexture, i);
                    BumpMapsArray.Set(m.normalMap, i);
                    MADS_Array.Set(m.MADS, i);
                    AlbedoArray.SetTextureArray();
                    BumpMapsArray.SetTextureArray();
                    MADS_Array.SetTextureArray();

                    if (i < 5) 
                    {
                        tiling[i] = new Vector4(m.Tiling, m.Tiling);
                    }
                }

                SPLAT_TILING.GlobalValue = tiling;

                return;
            }

            TerrainLayer[] prots = new TerrainLayer[masks.Count]; // terrain.terrainData.terrainLayers;

            for (var i = 0; i < masks.Count; i++)
            {
                prots[i] = masks[i].UpdateTexturesToLayers(i);
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

            if (terrainControlTexture)
                TerrainControlMain.GlobalValue = terrainControlTexture;
        }

        private void CreateTerrainHeightTexture()
        {

          //  TextureValue field = TerrainHeight;

            var size = terrain.terrainData.heightmapResolution - 1;

            terrainHeightTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = gameObject.scene.name + "-" + TerrainHeight.ToString()
            };

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

        private readonly pegi.EnterExitContext _context = new();

        void IPEGI.Inspect()
        {
            using (_context.StartContext())
            {
                var anyChanges = pegi.ChangeTrackStart();

                pegi.Nl();

                if (_context.IsAnyEntered == false)
                {
                    var changed = pegi.ChangeTrackStart();

                    "Terrain".PegiLabel().Edit_IfNull(ref terrain, gameObject);

                    if (!terrain && Icon.NewTexture.Click())
                        CreateTerrainHeightTexture();

                    pegi.Nl();

                    "Control Texture".PegiLabel().Edit(ref terrainControlTexture).Nl();

                    if (!terrainHeightTexture)
                    {
                        pegi.Click(CreateTerrainHeightTexture);
                    }
                    else
                    {


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

                if ("Height Texture".PegiLabel().IsEntered().Nl())
                {
                    "Height Texture".PegiLabel(70).Edit(ref terrainHeightTexture).Nl();


                    if (terrain)
                    {
                        if ("Tex2D To Terrain".PegiLabel().ClickConfirm(confirmationTag: "Load Terrain From Texture").Nl())
                            TerrainHeightTexture_To_UnityTerrain();

                        if ("Terrain To Tex2D".PegiLabel().Click().Nl())
                            UnityTerrain_To_HeightTexture();
                    }
                }


                if ("Splat".PegiLabel().IsEntered().Nl())
                {
                    var changed = pegi.ChangeTrackStart();

                    "Layers".PegiLabel().Edit_Inspect(ref mergeSubMasks).Nl();

                    if (mergeSubMasks && changed)
                    {
                        UpdateSplatTextures();
                    }
                }

                if ("Arrays".PegiLabel().IsEntered().Nl()) 
                {
                    AlbedoArray.Nested_Inspect().Nl();
                    BumpMapsArray.Nested_Inspect().Nl();
                    MADS_Array.Nested_Inspect().Nl();
                }

                if (anyChanges)
                    UpdateShaderValues();

            }
        }

        public string NeedAttention()
        {
            if (!terrainHeightTexture)
                return "No Height Texture";

            if (terrainHeightTexture.wrapMode != TextureWrapMode.Clamp)
                return "terrain Height Wrap better be Clamp";

                return null;
        }


        #endregion




    }


    [PEGI_Inspector_Override(typeof(MergingTerrainController))] internal class MergingTerrainEditor : PEGI_Inspector_Override { }

}