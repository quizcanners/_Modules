using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace QuizCanners.Modules
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU_MODULES + "Textures/" + FILE_NAME)]
    public class SO_MOHSGenerator : ScriptableObject, IPEGI
    {
        const string FILE_NAME = "MOHS Texture Set";

#if UNITY_EDITOR

        [Header("Turn this on to speed up repeated packing")]
        [SerializeField] private bool _keepReadable;

        [SerializeField] private string _textureName;

        [SerializeField] private MapChannel Metalness;
        [SerializeField] private MapChannel Occlusion;
        [SerializeField] private MapChannel Height;
        [SerializeField] private MapChannel Specular;

        [Header("If not NULL, it will be overriden")]
        [SerializeField] private Texture2D Result;

        private static bool s_keepReadale;

        public void CombineMasks()
        {
            // We will reference Occlusion for Texture Meta data. But can be any other one.
            var origin = Occlusion.Texture;

            if (!origin)
            {
                Debug.LogError("Occlusion texture not found");
                return;
            }

            s_keepReadale = _keepReadable;

            int width = origin.width;
            int height = origin.height;

            if (!Result)
            {
                Result = new Texture2D(width: width, height: height, textureFormat: TextureFormat.ARGB32, mipChain: true, linear: true)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    alphaIsTransparency = false,
                };
            }
            else
            {
                var preImporter = Result.GetTextureImporter_Editor();

                var needReimportOld = preImporter.WasNotReadable_Editor();
                needReimportOld |= preImporter.WasWrongIsColor_Editor(targetIsColor: false);
                needReimportOld |= preImporter.WasClamped_Editor();

                if (needReimportOld)
                    preImporter.SaveAndReimport();
            }

            Color32[] pixels = new Color32[width * height];

            PackageMetalness();
            PackageAO();
            PackageHeight();
            PackageSpecular();

            void PackageMetalness()
            {
                // Metalness

                if (Metalness.Texture)
                {
                    //Color32[] m_pix = Metalness.GetPixels32();

                    var vals = Metalness.GetChannel();

                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i].r = vals[i];
                }
                else
                {
                    byte taretValue = (byte)(Metalness.FallbackValue01 * 255.9);
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i].r = taretValue;
                }
            }
            void PackageAO()
            {
                if (Occlusion.Texture)
                {
                    //Color32[] m_pix = Occlusion.GetPixels32();
                    var vals = Occlusion.GetChannel();

                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i].g = vals[i];
                }
                else
                {
                    byte taretValue = (byte)(Occlusion.FallbackValue01 * 255.9);
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i].g = taretValue;
                }
            }
            void PackageHeight()
            {
                if (Height.Texture)
                {
                    var vals = Height.GetChannel();

                    for (int i = 0; i < pixels.Length; i++)
                    pixels[i].b = vals[i];
                    

                    return;
                }

                byte taretValue = (byte)(Height.FallbackValue01 * 255.9);
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i].b = taretValue;
            }
            void PackageSpecular()
            {
                if (Specular.Texture)
                {
                    var vals = Specular.GetChannel();

                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i].a = vals[i];

                    return;
                }

                byte taretValue = (byte)(Specular.FallbackValue01 * 255.9);
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i].a = taretValue;

            }


            Result.SetPixels32(pixels);

            QcUnity.SaveTextureSameFolder(ref Result, origin, _textureName);

            /*
            var bytes = Result.EncodeToPNG();
            var dest = ReplaceFirst(text: AssetDatabase.GetAssetPath(origin), search: "Assets", replace: ""); // AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "", 1);// AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "");
            var extension = dest[(dest.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

            dest = dest[..^extension.Length] + "png";

            dest = ReplaceLastOccurrence(dest, origin.name, _textureName);

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();

            Result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            var importer = Result.GetTextureImporter_Editor();

            var needReimport = importer.WasReadable_Editor();
            needReimport |= importer.WasWrongIsColor_Editor(targetIsColor: false);
            needReimport |= importer.WasClamped_Editor();

            if (needReimport)
                importer.SaveAndReimport();
            */
            return;

            /*
            static string ReplaceFirst(string text, string search, string replace)
            {
                int pos = text.IndexOf(search);
                if (pos < 0)
                {
                    return text;
                }
                return text[..pos] + replace + text[(pos + search.Length)..];
            }

            static string ReplaceLastOccurrence(string source, string find, string replace)
            {
                var place = source.LastIndexOf(find, StringComparison.Ordinal);

                if (place == -1)
                    return source;

                var result = source.Remove(place, find.Length).Insert(place, replace);
                return result;
            }*/
        }


        #region Inspector
        public override string ToString() => string.Format("{0} ({1})", _textureName, this.name);

        void IPEGI.Inspect()
        {
            pegi.TryDefaultInspect(this);

            "Copy name".PegiLabel().Click().Nl(()=> _textureName = Occlusion.ToString() + "_MOHS");
            pegi.Click(CombineMasks).Nl();



        }

        #endregion

        /// <summary>
        /// Data for a single channel for the targeted texture.
        /// </summary>
        [Serializable]
        private class MapChannel
        {
            public Texture2D Texture;
            [Range(0, 1)]
            public float FallbackValue01 = 1;
            public ColorChanel FromChannel = ColorChanel.R;

            public override string ToString() => Texture ? Texture.name : "NO_TEXTURE";

            public byte[] GetChannel()
            {
                var pixes = GetPixels32();

                byte[] values = new byte[pixes.Length];

                switch (FromChannel) 
                {
                    case ColorChanel.R:

                        for (int i = 0; i < pixes.Length; i++)
                            values[i] = pixes[i].r;
                    break;

                    case ColorChanel.G:

                        for (int i = 0; i < pixes.Length; i++)
                            values[i] = pixes[i].g;
                        break;

                    case ColorChanel.B:

                        for (int i = 0; i < pixes.Length; i++)
                            values[i] = pixes[i].b;
                        break;

                    case ColorChanel.A:

                        for (int i = 0; i < pixes.Length; i++)
                            values[i] = pixes[i].a;
                        break;
                }

                return values;
            }

            public Color32[] GetPixels32()
            {
                CheckSourceTexture();
                var pixels = Texture.GetPixels32();

                if (!s_keepReadale)
                    Texture.MakeNotReadable();

                return pixels;
            }

            void CheckSourceTexture()
            {
                if (!Texture)
                    return;

                var importer = Texture.GetTextureImporter_Editor();

                var needReimport = importer.WasNotReadable_Editor();
                needReimport |= QcUnity.WasWrongIsColor_Editor(importer, targetIsColor: false);

                if (needReimport)
                    importer.SaveAndReimport();
            }
        }



#endif

    }

    [PEGI_Inspector_Override(typeof(SO_MOHSGenerator))]
    internal class SO_MOHSGeneratorDrawer : PEGI_Inspector_Override { }

    public static class TexturePackingUtils
    {
#if UNITY_EDITOR
        public static void MakeNotReadable(this Texture2D texture)
        {
            var importer = texture.GetTextureImporter_Editor();

            if (importer.isReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        public static TextureImporter GetTextureImporter_Editor(this Texture tex) =>
               AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;

        public static bool WasNotReadable_Editor(this TextureImporter importer)
        {
            var needsReimport = false;

            if (importer.isReadable == false)
            {
                importer.isReadable = true;
                needsReimport = true;
            }

            if (importer.textureType == TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Default;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            return needsReimport;


        }

        /*
        public static bool WasReadable_Editor(this TextureImporter importer)
        {
            var needsReimport = false;

            if (importer.isReadable)
            {
                importer.isReadable = false;
                needsReimport = true;
            }

            return needsReimport;
        }


        public static bool WasClamped_Editor(this TextureImporter importer)
        {

            var needsReimport = false;


            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static bool WasWrongIsColor_Editor(this TextureImporter importer, bool targetIsColor)
        {

            var needsReimport = false;

            if (importer.sRGBTexture != targetIsColor)
            {
                importer.sRGBTexture = targetIsColor;
                needsReimport = true;
            }

            return needsReimport;
        }
        */
#endif

    }

}