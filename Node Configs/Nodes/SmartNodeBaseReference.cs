using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame.NodeNotes
{

    public partial class SO_ConfigBook
    {
        [Serializable]
        public class BookReference : SmartStringIdGeneric<SO_ConfigBook>,  IPEGI
        {
            public bool IsReferenceOf(SO_ConfigBook book) => book.NameForInspector.Equals(Id);
            protected override Dictionary<string, SO_ConfigBook> GetEnities() => Singleton.Get<Singleton_ConfigNodes>().books;
            public BookReference(SO_ConfigBook book) 
            {
                if (book!= null)
                    Id = book.NameForInspector;
            }

            public BookReference() { }
        }

        public partial class Node 
        {
            [Serializable]
            public class Reference : IPEGI, IPEGI_ListInspect, IGotReadOnlyName, INeedAttention
            {
                [SerializeField] private BookReference _book;
                [SerializeField] public int NodeIndex = -1;
                [SerializeField] private int _treeVersion;

                [NonSerialized] private NodesChain _cachedChain;

                public SO_ConfigBook Book 
                {
                    get => _book.GetEntity();
                    set 
                    {
                        _book = new BookReference(value);
                    }
                }

                public NodesChain GenerateNodeChain()
                {
                    if (_cachedChain == null)
                        Singleton.Try<Singleton_ConfigNodes>(s => _cachedChain = s[this]);
                    
                    return _cachedChain;
                }

                public bool IsReferenceTo(Node node) => node != null && node._index == NodeIndex;
                public bool SameAs(Reference reff) => reff._book.SameAs(_book) && reff.NodeIndex == NodeIndex;
                public SO_ConfigBook GetBook() => _book.GetEntity();

                #region Inspector

                private int _inspectedStuff = -1;
                public void Inspect()
                {
                    var book = GetBook();

                    if (book == null || "Book ({0})".F(_book.GetReadOnlyName()).PegiLabel().IsEntered(ref _inspectedStuff, 0).Nl())
                        _book.Inspect();
                    
                    var chain = GenerateNodeChain();

                    if ("Node ({0})".F(chain.GetNameForInspector()).PegiLabel().IsEntered(ref _inspectedStuff, 1))
                    {
                        pegi.Nl();
                        if (book != null)
                            "Node".PegiLabel().Select_iGotIndex(ref NodeIndex, book.GetAllNodes());
                                
                        if (chain != null)
                            chain.Nested_Inspect();
                    }

                    pegi.Nl();
                }

                public void InspectInList(ref int edited, int ind)
                {
                    var book = _book.GetEntity();

                    if (!book)
                        _book.InspectInList(ref edited, ind);
                    else 
                    {
                        "Node".PegiLabel(60).Select_iGotIndex(ref NodeIndex, book.GetAllNodes());

                        if (Icon.Enter.Click())
                            edited = ind;
                    }   
                }

                public string GetReadOnlyName()
                {
                    var n = GenerateNodeChain();

                    if (n != null)
                        return n.GetNameForInspector();

                    var b = GetBook();
                    if (b)
                        return "{0}-> ???".F(b.GetNameForInspector());

                    return "NO BOOK";
                }

                public string NeedAttention()
                {
                    var b = _book.NeedAttention();

                    if (b.IsNullOrEmpty() == false)
                        return b;

                    if (GenerateNodeChain().LastNode == null)
                        return "Node {0} not found".F(NodeIndex);

                    return null;
                }
                #endregion


                public Reference(Node node, SO_ConfigBook book)
                {
                    NodeIndex = node == null ? -1 : node.IndexForInspector;
                    _book = new BookReference(book);
                }
                public Reference(SO_ConfigBook book)
                {
                    NodeIndex = -1;
                    _book = new BookReference(book);
                }

                public Reference()
                {
                    _book = new BookReference();
                }
            }
        }
    }
}

