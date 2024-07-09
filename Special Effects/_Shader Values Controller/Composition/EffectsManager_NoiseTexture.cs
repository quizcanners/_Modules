using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEditor;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public static partial class Effects 
    { 
        [System.Serializable]
        public class NoiseTextureManager : IPEGI, INeedAttention, IPEGI_ListInspect
        {
            private readonly ShaderProperty.Feature _noiseTexture = new("USE_NOISE_TEXTURE");
            private readonly ShaderProperty.TextureValue _noiseTextureGlobal = new("_Global_Noise_Lookup");
            private readonly ShaderProperty.TextureValue _noiseTextureGlobal3D = new("_Global_Noise_Lookup3D");

            [SerializeField] private bool _enableNoise = true;
            [SerializeField] private Texture2D _prerenderedNoiseTexture;
            [SerializeField] private Texture3D _prerenderedNoiseTexture3D;

            public bool EnableNoise
            {
                get => _enableNoise;
                set
                {
                    if (_enableNoise != value)
                    {
                        _enableNoise = value;
                        UpdateShaderGlobal();
                    }
                }
            }

            private void UpdateShaderGlobal()
            {
                _noiseTexture.Enabled = _enableNoise && _prerenderedNoiseTexture;
                _noiseTextureGlobal.SetGlobal(_prerenderedNoiseTexture);
                _noiseTextureGlobal3D.SetGlobal(_prerenderedNoiseTexture3D);
            }

            public void ManagedOnEnable() => UpdateShaderGlobal();


            #region Inspector

            public string NeedAttention()
            {
                if (!_prerenderedNoiseTexture)
                    return "No Texture";

#if UNITY_EDITOR
                var importer = _prerenderedNoiseTexture.GetTextureImporter_Editor();

                if (importer.alphaIsTransparency)
                    return "Texture Alpha shouldn't be transparency";
                if (importer.filterMode != FilterMode.Point)
                    return "Filter Mode should be Point";
                if (importer.wrapMode != TextureWrapMode.Repeat)
                    return "Wrap Mode should be repeat";
                if (importer.textureCompression != UnityEditor.TextureImporterCompression.Uncompressed)
                    return "Texture should be uncompressed";
                if (importer.sRGBTexture)
                    return "Texture shouldn't be an RGB Texture";

#endif

                return null;
            }

            void IPEGI.Inspect()
            {
                var changed = pegi.ChangeTrackStart();

                pegi.FullWindow.DocumentationClickOpen("This component will set noise texture as a global parameter. Using texture is faster then generating noise in shader.", "About Noise Texture Manager");

                pegi.Nl();

                "Noise Tex".PegiLabel(120).Edit(ref _prerenderedNoiseTexture);

                if (_prerenderedNoiseTexture)
                    Icon.Refresh.Click("Update value in shader");

                pegi.Nl();

                if (_prerenderedNoiseTexture)
                    _noiseTexture.ToString().PegiLabel().ToggleIcon(ref _enableNoise, hideTextWhenTrue: true);

                if (_enableNoise)
                {
                    "Compile Directive and Global Texture:".PegiLabel().Nl();

                    _noiseTexture.ToString().PegiLabel().Write_ForCopy(showCopyButton: true).Nl();
                    _noiseTextureGlobal.ToString().PegiLabel().Write_ForCopy(showCopyButton: true).Nl();
                    _noiseTextureGlobal3D.ToString().PegiLabel().Write_ForCopy(showCopyButton: true).Nl();
                }
                pegi.Nl();

                "Noise Tex3D".PegiLabel(120).Edit(ref _prerenderedNoiseTexture3D);

                if ("Generate 3D Texture".PegiLabel("Will take a bit. Ready to wait?").ClickConfirm())
                    _prerenderedNoiseTexture3D = Noise.CreateTexture3D();

                pegi.Nl();

                if (changed)
                    UpdateShaderGlobal();
            }

            public void InspectInList(ref int edited, int index)
            {
                var en = EnableNoise;
                if (pegi.ToggleIcon(ref en))
                    EnableNoise = en;


                if (ToString().PegiLabel().ClickLabel() | this.Click_Enter_Attention())
                    edited = index;
            }

            public override string ToString() => "Noise Texture";
            #endregion
        }


        public static class Noise 
        {
            private static double Sample3DHashUI32(uint x, uint y, uint z)
            {
                // Pick some enthropy source values.
                // Try different values.
                const uint enthropy0 = 1200u;
                const uint enthropy1 = 4500u;
                const uint enthropy2 = 6700u;
                const uint enthropy3 = 8900u;

                // Use linear offset method to mix coordinates.
                uint value =
                    z * enthropy3 * enthropy2 +
                    y * enthropy2 +
                    x;

                // Calculate hash.
                value += enthropy1;
                value *= 445593459u;
                value ^= enthropy0;

                // 1.0f / 4294967295.0f = 2.32830644e-10

                return ((double)(value * value * value)) * 2.32830644e-10;
            }

            private static double WorleyNoise3D(float u, float v, float w)
            {
                // Fractial part.
                float fractU = u - Mathf.Floor(u);
                float fractV = v - Mathf.Floor(v);
                float fractW = w - Mathf.Floor(w);

                // Integer part.
                u = Mathf.Floor(u);
                v = Mathf.Floor(v);
                w = Mathf.Floor(w);

                double minDistance = 3.40282347e+37f; // FL_MAX = 3.40282347e+38f

                for (int z = -1; z < 2; z++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        for (int x = -1; x < 2; x++)
                        {
                            // Pseudorandom sample coordinates in corresponding cell.
                            double xSample = x + Sample3DHashUI32((uint)(u + x), (uint)(v + y), (uint)(w + z));
                            double ySample = y + Sample3DHashUI32((uint)(u + x), (uint)(v + y), (uint)(w + z));
                            double zSample = z + Sample3DHashUI32((uint)(u + x), (uint)(v + y), (uint)(w + z));

                            double distance =
                                    (fractU - xSample) * (fractU - xSample) +
                                    (fractV - ySample) * (fractV - ySample) +
                                    (fractW - zSample) * (fractW - zSample);

                            // Mistance from pixel to pseudorandom sample.
                            minDistance = Math.Min(minDistance, distance);
                        }
                    }
                }

                return minDistance;
            }

            private static Vector3 FbmW(Vector3 c)
            {

                double gray = WorleyNoise3D(c.x, c.y, c.z) * 0.35f;
                gray += WorleyNoise3D(c.x * 2.054f, c.y * 2.354f, c.z * 2.754f) * 0.125f;
                gray += WorleyNoise3D(c.x * 4.554f, c.y * 4.254f, c.z * 4.154f) * 0.025f;
                gray += WorleyNoise3D(c.x * 32.554f, c.y * 32.354f, c.z * 32.430f) * 0.025f;

                return new Vector3((float)(c.x + gray), (float)(c.y + gray), (float)(c.z));
            }

            private static double FbmW2(Vector3 c)
            {
                double gray = WorleyNoise3D(c.x, c.y, c.z) * 0.35f;
                gray += WorleyNoise3D(c.x * 2.054f, c.y * 2.354f, c.z * 2.754f) * 0.125f;
                gray += WorleyNoise3D(c.x * 4.554f, c.y * 4.254f, c.z * 4.154f) * 0.025f;
                gray += WorleyNoise3D(c.x * 32.554f, c.y * 32.354f, c.z * 32.430f) * 0.025f;

                return gray;
            }

            internal static Texture3D CreateTexture3D()
            {
                // Configure the texture
                int size = 32;
                TextureFormat format = TextureFormat.RGBA32;
                TextureWrapMode wrapMode = TextureWrapMode.Clamp;

                // Create the texture and apply the configuration
                Texture3D texture = new(size, size, size, format, false)
                {
                    wrapMode = wrapMode
                };

                // Create a 3-dimensional array to store color data
                Color32[] colors = new Color32[size * size * size];


                //fbmW2(fbmW(fbmW(coords * 0.5) + fbmW(coords * 0.2))); } // RB
                // float gray = WorleyNoise3D(c.x, c.y, c.z) * 0.35f;
                // gray += WorleyNoise3D(c.x * 2.054f, c.y * 2.354f, c.z * 2.754f) * 0.125f;
                // gray += WorleyNoise3D(c.x * 4.554f, c.y * 4.254f, c.z * 4.154f) * 0.025f;
                // gray += WorleyNoise3D(c.x * 32.554f, c.y * 32.354f, c.z * 32.430f) * 0.025f;
                //  float inverseResolution = 1.0f / (size - 1.0f);

                double maxValue = 0.001;


                for (int z = 0; z < size; z++)
                {
                    int zOffset = z * size * size;
                    for (int y = 0; y < size; y++)
                    {
                        int yOffset = y * size;
                        for (int x = 0; x < size; x++)
                        {
                            var col = new Color32();//new Color32(0, (byte)(UnityEngine.Random.value * 255), (byte)(UnityEngine.Random.value * 255), (byte)(UnityEngine.Random.value * 255));

                            //Red: Warped Worley noise 3D
                            Vector3 coords = new(x, y, z);
                            //   coords.Scale(new Vector3(1f/x, 1f/y, 1f/z));

                            //   double grey = 2 * FbmW2(FbmW(FbmW(0.5f * coords) + FbmW(0.2f * coords)));

                            col.r = (byte)(255 * UnityEngine.Random.value); //Get();//(byte)(255 * grey);
                            coords.Scale(Vector3.one * 0.5f);
                            col.g = Get();
                            coords.Scale(Vector3.one * 0.5f);
                            col.b = Get();
                            coords.Scale(Vector3.one * 0.5f);
                            col.a = Get();
                            //col.a = col.r;

                            byte Get()
                            {
                                var val = FbmW2(FbmW(FbmW(0.5f * coords) + FbmW(0.2f * coords)));
                                maxValue = Math.Max(maxValue, val);
                                return (byte)(255 * val);
                            }

                            colors[x + yOffset + zOffset] = col;
                        }
                    }
                }

                if (maxValue < 1)
                {
                    Debug.Log("Max Value:" + maxValue);

                    float coef = (float)(1f / maxValue);

                    for (int i = 0; i < colors.Length; i++)
                    {
                        Color col = colors[i];
                        col = new Color(col.r * coef, col.g * coef, col.b * coef, col.a * coef);

                        colors[i] = col;
                    }
                }

                texture.wrapMode = TextureWrapMode.Repeat;

                texture.SetPixels32(colors);

                texture.Apply();

#if UNITY_EDITOR
                AssetDatabase.CreateAsset(texture, "Assets/Example3DTexture.asset");
#endif

                return texture;
            }
        }
    }

}
