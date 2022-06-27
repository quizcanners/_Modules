using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace QuizCanners.IsItGame
{
    public class SO_EnumeratedAssetReferenceBase: ScriptableObject 
    {
        [SerializeField] internal List<EnumeratedReference> references;

        [Serializable]
        public class EnumeratedReference : AddressableBase, IPEGI_ListInspect
        {
           
            public AssetReference Reference;
          
            [NonSerialized] protected AsyncOperationHandle _handle;
            protected override AsyncOperationHandle GetHandle() => _handle;
            public Task GetAsync<T>(Action<T> onExit = null) where T : Object => GetAsync_Internal(onExit);
                
            protected override void StartLoad()
            {
                if (!DirectReference)
                {
                    _handle = GetReference().LoadAssetAsync<Object>();
                }
            }

            public bool IsReferenceVaid => Reference.AssetGUID.IsNullOrEmpty() == false;

            protected override AssetReference GetReference() => Reference;

            #region Inspector
            public static Object inspectedDataSource;
            public static Type inspectedEnum;

            public void InspectInList(ref int edited, int ind)
            {
                string name = Enum.ToObject(inspectedEnum, ind).ToString();

                name.PegiLabel().Nl();

               /* if (Name.IsNullOrEmpty())
                {
                    "Set name".PegiLabel().Click(() => Name = name);
                }
                else if (!name.Equals(Name))
                    Icon.Refresh.Click(() => Name = name, "Refresh Name");
                   
                Name.PegiLabel(90).Write();*/

                if (DirectReference && Reference!= null && Reference.AssetGUID.IsNullOrEmpty() == false && Icon.Clear.Click("Clear Reference"))
                    Reference = null;
                
                if (!DirectReference || Reference.IsValid())
                {
                    "Adressable".PegiLabel(90).Edit_Property(
                        () => Reference,
                        nameof(references),
                        inspectedDataSource);
                }

                "Assets".PegiLabel(60).Edit(ref DirectReference);
                pegi.Line();
            }

            public override string ToString()
            {
                if (DirectReference)
                    return DirectReference.GetNameForInspector();

                if (Reference.IsValid())
                    return "Addressable";

                return "Empty";
            }

            #endregion
        }
    }

    public class EnumeratedAssetReferences<T,G> : SO_EnumeratedAssetReferenceBase, IPEGI where T : struct, IComparable, IFormattable, IConvertible where G : Object
    {
        public EnumeratedReference GetReference(T key) 
        {
            var reff = references.TryGet(Convert.ToInt32(key));
            return reff;
        }

        public Task GetAssync(T key, Action<G> onExit) 
        {
            var reff = references.TryGet(Convert.ToInt32(key));

            if (reff!= null) 
            {
                return reff.GetAsync(onExit: onExit);
            } else 
            {
                try
                {
                    onExit?.Invoke(null);
                } catch(Exception ex) 
                {
                    Debug.LogException(ex);
                }
                return Task.CompletedTask;
            }
        }

        #region Inspector

        private int _inspectedReference = -1;
        public void Inspect()
        {
            EnumeratedReference.inspectedEnum = typeof(T);
            EnumeratedReference.inspectedDataSource = this;

            (typeof(T).ToPegiStringType() + "s").PegiLabel().Edit_List(references, ref _inspectedReference);
        }

        #endregion
    }

}
