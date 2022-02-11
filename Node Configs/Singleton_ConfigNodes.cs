using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.IsItGame.NodeNotes
{
    [ExecuteAlways]
    public partial class Singleton_ConfigNodes : Singleton.BehaniourBase, IPEGI
    {
        public const string NODE_CONFIGS = "Node Configs";

        [SerializeField] internal BooksDictionary books = new();
        [SerializeField] private SO_ConfigBook.Node.Reference _reference;

        [NonSerialized] public int Version;

        internal void SetChainDirty() => Version++;

        public void SetCurrent(SO_ConfigBook.NodesChain chain)
        {
            _reference = chain.GetReferenceToLastNode();
            SetChainDirty();
        }

        public void SetCurrent(SO_ConfigBook.Node.Reference reff)
        {
            _reference = reff;
            SetChainDirty();
        }

        public bool IsCurrent(SO_ConfigBook.Node node) => _reference != null && _reference.IsReferenceTo(node);
        public bool IsCurrent(SO_ConfigBook.Node.Reference reff) => _reference != null && _reference.SameAs(reff);

        public bool AnyEntered
        {
            get
            {
                if (_reference == null)
                    return false;

                var c = _reference.GenerateNodeChain();

                return c != null && c.LastNode != null;
            }
        }

        [Serializable]
        internal class BooksDictionary: SerializableDictionary<string, SO_ConfigBook> 
        {
            protected override pegi.CollectionInspectorMeta CollectionMeta
            {
                get
                {
                    if (_collectionMeta == null)
                    {
                        _collectionMeta = new pegi.CollectionInspectorMeta(labelName: "Node Books", showAddButton: false) { ElementName = ElementName };
                    }
                    return _collectionMeta;
                }
            }


            public override void Inspect()
            {
                base.Inspect();

                if (!CollectionMeta.IsAnyEntered)
                {
                    SO_ConfigBook tmp = null;
                    if ("Add Book".PegiLabel(90).Edit(ref tmp).Nl() && tmp && ContainsKey(tmp.name) == false)
                        Add(tmp.name, tmp);
                }
            }
        }

        public SO_ConfigBook.NodesChain this[SO_ConfigBook.Node.Reference rff] 
        {
            get {
                var book = rff.GetBook();
                return book ? book[rff] : null;
            }
        }

        public SO_ConfigBook.NodesChain CurrentChain => this[_reference];

        #region Inspect

        public override string InspectedCategory => Utils.Singleton.Categories.GAME_LOGIC;

        private int _inspectedCategory = -1;
        public override void Inspect()
        {
            var chain = CurrentChain;

            if (chain != null && chain.LastNode != null && "Save -> {0}".F(chain.LastNode).PegiLabel().Click().Nl())
                chain.SaveConfigsOfServicesToChain();

            pegi.Nl();

            var changes = pegi.ChangeTrackStart();

            int category = -1;

            if ("Curren Node".PegiLabel().IsEntered(ref _inspectedCategory, ++category).Nl())
            {
              

                chain.Nested_Inspect();
            }


            "Books".PegiLabel().Enter_Inspect(books, ref _inspectedCategory, ++category).Nl();


            

            if (changes)
                SetChainDirty();
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_ConfigNodes))] internal class ConfigNodesManagerDrawer : PEGI_Inspector_Override { }
}