using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.Modules.NodeNotes
{
    [ExecuteAlways]
    [AddComponentMenu(QcUtils.QUIZCANNERS + "/Config Nodes/Manager")]
    public partial class Singleton_ConfigNodes : Singleton.BehaniourBase, IPEGI
    {
        public const string NODE_CONFIGS = "Node Configs";

        [SerializeField] internal BooksDictionary books = new();
        private SO_ConfigBook.Node.Id _currentNode;
        [SerializeField] private SO_ConfigBook.Node.Id _startingNode;

        [NonSerialized] public int Version;
        private readonly Gate.Integer _nodeNodesConfigVersion = new();

        internal void SetChainDirty()
        {
            Version++;

            if (Application.isEditor && _currentNode!= null)
                _currentNode.Book.SetToDirty();
        }
        public void SetDefaultNode() => SetCurrent(_startingNode);

        public void SetCurrent(SO_ConfigBook.NodesChain chain)
        {
            _currentNode ??= new SO_ConfigBook.Node.Id();
            _currentNode = chain.GetReferenceToLastNode();
            SetChainDirty();
        }

        public void SetCurrent(SO_ConfigBook.Node.Id reff)
        {
            _currentNode ??= new SO_ConfigBook.Node.Id();
            _currentNode.CopyFrom(reff);
            SetChainDirty();
        }

        public bool IsCurrent(SO_ConfigBook.Node node) => _currentNode != null && _currentNode.IsReferenceTo(node);
        public bool IsCurrent(SO_ConfigBook.Node.Id reff) => _currentNode != null && _currentNode.SameAs(reff);

        public bool AnyEntered
        {
            get
            {
                if (_currentNode == null)
                    return false;

                var c = _currentNode.GenerateNodeChain();

                return c != null && c.LastNode != null;
            }
        }

        public SO_ConfigBook.NodesChain this[SO_ConfigBook.Node.Id nodeReference]
        {
            get
            {

                if (nodeReference == null)
                    return null;

                var book = nodeReference.GetBook();
                return book ? book[nodeReference] : null;
            }
        }

        public SO_ConfigBook.NodesChain CurrentChain => this[_currentNode];

        protected override void OnAfterEnable()
        {
            base.OnAfterEnable();
            if (Application.isPlaying && AnyEntered == false)
                SetDefaultNode();
        }

        void Update() 
        {
            if (AnyEntered && Application.isPlaying)
            {
                if (_nodeNodesConfigVersion.TryChange(Version)) 
                    CurrentChain.LoadConfigsIntoServices();
            }
        }

        #region Inspect

        public override string InspectedCategory => Utils.Singleton.Categories.GAME_LOGIC;

        private readonly pegi.EnterExitContext _context = new();

        public override void Inspect()
        {
            using (_context.StartContext())
            {

                var chain = CurrentChain;

                if (chain != null && chain.LastNode != null && "Save -> {0}".F(chain.LastNode).PegiLabel().Click().Nl())
                    chain.SaveConfigsOfServicesToChain();

                pegi.Nl();

                var changes = pegi.ChangeTrackStart();

                if (chain != null && Icon.Clear.Click("Delete Chain"))
                    _currentNode = null;

                "Chain: {0}".F(chain.GetNameForInspector()).PegiLabel().Enter_Inspect(chain);

                if (_context.IsAnyEntered == false && !_startingNode.SameAs(_currentNode))
                {
                    pegi.Click(SetDefaultNode);
                }

                pegi.Nl();

                "Default Node: {0}".F(_startingNode.GetNameForInspector()).PegiLabel().Conditionally_Enter_Inspect(canEnter: !Application.isPlaying, _startingNode).Nl();

                "Books".PegiLabel().Enter_Inspect(books).Nl();

                if (changes)
                    SetChainDirty();
            }
        }
        #endregion

        [Serializable]
        internal class BooksDictionary: SerializableDictionary<string, SO_ConfigBook> 
        {
            protected override pegi.CollectionInspectorMeta CollectionMeta
            {
                get
                {
                    _collectionMeta ??= new pegi.CollectionInspectorMeta(labelName: "Node Books", showAddButton: false) { ElementName = ElementName };
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



    }

    [PEGI_Inspector_Override(typeof(Singleton_ConfigNodes))] internal class ConfigNodesManagerDrawer : PEGI_Inspector_Override { }
}