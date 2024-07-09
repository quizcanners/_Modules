using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Modules.NodeNotes
{

    public partial class SO_ConfigBook
    {
        [Serializable]
        public class BookReference : SmartId.StringGeneric<SO_ConfigBook>,  IPEGI
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
            public class Id : IPEGI, IPEGI_ListInspect, INeedAttention
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

                public void CopyFrom(Id other) 
                {
                    Book = other.Book;
                    NodeIndex = other.NodeIndex;
                    _treeVersion = other._treeVersion;
                }
                public bool IsReferenceTo(Node node) => node != null && node._index == NodeIndex;
                public bool SameAs(Id reff) => reff != null && reff._book.Equals(_book) && reff.NodeIndex == NodeIndex;
                public SO_ConfigBook GetBook() => _book.GetEntity();

                #region Inspector

                // private int _inspectedStuff = -1;

                private readonly pegi.EnterExitContext context = new();

                void IPEGI.Inspect()
                {
                    using (context.StartContext())
                    {
                        var changes = pegi.ChangeTrackStart();

                        var book = GetBook();

                        if (book == null || "Book ({0})".F(_book.ToString()).PegiLabel().IsEntered().Nl())
                            _book.Inspect();

                        var chain = GenerateNodeChain();

                        if ("Node ({0})".F(chain.GetNameForInspector()).PegiLabel().IsEntered())
                        {
                            pegi.Nl();
                            if (book != null)
                                "Node".PegiLabel().Select_iGotIndex(ref NodeIndex, book.GetAllNodes());

                            chain?.Nested_Inspect().OnChanged(() => CopyFrom(chain.GetReferenceToLastNode()));
                        }

                        pegi.Nl();

                        if (changes)
                            _cachedChain = null;
                    }
                }

                public void InspectInList(ref int edited, int ind)
                {
                    var changes = pegi.ChangeTrackStart();

                    var book = _book.GetEntity();

                    if (!book)
                        _book.InspectInList(ref edited, ind);
                    else 
                    {
                        if (Icon.Clear.ClickConfirm(confirmationTag: "ClearBook"))
                        {
                            _book = new BookReference();
                            NodeIndex = -1;
                        }

                        "Node".PegiLabel(60).Select_iGotIndex(ref NodeIndex, book.GetAllNodes());

                        if (Icon.Enter.Click())
                            edited = ind;
                    }

                    if (changes)
                        _cachedChain = null;
                }

                public override string ToString()
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


                public Id(Node node, SO_ConfigBook book)
                {
                    NodeIndex = node == null ? -1 : node.IndexForInspector;
                    _book = new BookReference(book);
                }
                public Id(SO_ConfigBook book)
                {
                    NodeIndex = -1;
                    _book = new BookReference(book);
                }

                public Id()
                {
                    _book = new BookReference();
                }
            }
        }
    }
}

