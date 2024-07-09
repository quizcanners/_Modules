using System.Collections;
using UnityEngine;
using QuizCanners.Inspect;
using UnityEngine.AddressableAssets;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;
using QuizCanners.Utils;

namespace QuizCanners.Modules.Audio
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU_MODULES + "Audio/" + FILE_NAME)]
    public class SO_Music_ClipData : ScriptableObject, IPEGI_ListInspect, IPEGI, INeedAttention
    {
        public const string FILE_NAME = "Song";

        public AssetReference Reference;
        public bool AlwaysStartFromBeginning;
        public float Volume = 1;

        [Header("Featuring Meta Data")]
        public bool FeatureInUi;
        public string BandName;
        public string AlbumName;
        public string SongName;

        [NonSerialized] private AsyncOperationHandle<AudioClip> _handle;
        [NonSerialized] private AudioClip clip;
        [NonSerialized] private bool failedToLoad;

        public AudioClip GetIfCached() => clip;

        public void Release() => Addressables.Release(_handle);

        public bool GotReference() => Reference != null && Reference.RuntimeKeyIsValid();

        public IEnumerator GetClipAsync(Action<AudioClip> onComplete = null)
        {
            if (failedToLoad || !GotReference())
            {
                Finalize();
                yield break;
            }

            if (_handle.IsValid())
            {
                yield return _handle;
                Finalize(_handle.Result);
                yield break;
            }

            try
            {
                _handle = Reference.LoadAssetAsync<AudioClip>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
                failedToLoad = true;
                Finalize();
                yield break;
            }

            _handle.Completed += action =>
            {
                if (action.IsDone)
                {
                    Finalize(action.Result);
                } else 
                {
                    failedToLoad = true;
                    Finalize();
                }
            };

            void Finalize(AudioClip newClip = null)
            {
                if (newClip)
                {
                    clip = newClip;
                }

                try
                {
                    onComplete?.Invoke(clip);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            yield return _handle;
        }

        #region Inspector

        public override string ToString() => FeatureInUi ? "{0} by {1}".F(SongName, BandName) : name;

        public void InspectInList(ref int edited, int ind)
        {
            if (!GotReference())
                Icon.InActive.Draw();

            name.PegiLabel(90).Edit_Property(() => Reference, this);

            pegi.ClickHighlight(this);

            if (Icon.Enter.Click())
                edited = ind;
        }

        void IPEGI.Inspect()
        {
            pegi.Nl();
            "Clip".PegiLabel(40).Edit_Property(() => Reference, this).Nl();
            "Always From Start".PegiLabel().ToggleIcon(ref AlwaysStartFromBeginning).Nl();
            "Volume".PegiLabel(50).Edit_01(ref Volume).Nl();

            "Feature Band".PegiLabel().ToggleIcon(ref FeatureInUi).Nl();
            if (FeatureInUi)
            {
                "Band".PegiLabel(50).Edit(ref BandName).Nl();
                "Album".PegiLabel(50).Edit(ref AlbumName).Nl();
                "Song".PegiLabel(50).Edit(ref SongName).Nl();
            }
        }

        public string NeedAttention()
        {
            if (!GotReference())
                return "No Clip";

            return null;

        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(SO_Music_ClipData))]
    internal class SO_Music_ClipDataDrawer : PEGI_Inspector_Override { }

}