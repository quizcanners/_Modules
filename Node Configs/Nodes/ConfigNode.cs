using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame.NodeNotes
{
    public partial class SO_ConfigBook
    {

        public partial class Node : IGotIndex, IGotName, IGotCount, ICfg, IPEGI_ListInspect, IPEGI
        {
            private string _name = "UNNAMED";
            private int _index;
            private List<Node> _childNodes = new();
            private ConfigsDictionary configs = new();

            public bool PopulateChainRecursively(Reference reff, NodesChain result) 
            {
                var token = result.AddAndUse(this);

                if (_index == reff.NodeIndex) 
                    return true;
                
                foreach (var n in _childNodes)
                    if (n.PopulateChainRecursively(reff, result))
                        return true;

                token.Dispose();

                return false;
            }

            internal void PopulateAllNodes(List<Node> list) 
            {
                list.Add(this);
                foreach (var n in _childNodes)
                    n.PopulateAllNodes(list);
            }

            #region Encode & Decode

            public CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add_String("n", _name)
                    .Add("i", _index)
                    .Add("c", _childNodes)
                    .Add("cfgs", configs);

                return cody;
            }


            public void DecodeTag(string key, CfgData data)
            {
                switch (key) {
                    case "n": _name = data.ToString(); break;
                    case "i": _index = data.ToInt(); break;
                    case "c": data.ToList(out _childNodes); break;
                    case "cfgs": data.ToDictionary(out configs); break;
                }
            }

            #endregion

            private Singleton_ConfigNodes Mgmt => Singleton.Get<Singleton_ConfigNodes>();


            public bool IsEntered 
            { 
                get 
                {
                    if (Mgmt.AnyEntered) 
                        return IsEntered_Internal();
                    
                    return false;
                } 
            }

            private bool IsEntered_Internal() 
            {
                if (Mgmt.IsCurrent(this))
                    return true;

                foreach (var ch in _childNodes)
                {
                    if (ch.IsEntered_Internal())
                        return true;
                }
                return false;
            }

            internal bool TryGetConfig (ITaggedCfg val, out CfgData dta) 
            {
                if (configs.TryGetValue(val.TagForConfig, out dta)) 
                    return true;

                return false;
            }

            public void SetConfigOnTheNode(ITaggedCfg val, CfgData dta)
            {
                configs[val.TagForConfig] = dta;
                Singleton.Try<Singleton_ConfigNodes>(s => s.SetToDirty());
            }

            public int IndexForInspector
            {
                get => _index;
                set
                {
                    _index = value;
                }
            }

            public string NameForInspector
            {
                get => _name;
                set => _name = value;
            }

            private void Initialize(SO_ConfigBook book) 
            {
                _index = book._freeNodeIndex;
                book._freeNodeIndex++;
                book.OnNodeTreeChanged();
            }

            internal Node(SO_ConfigBook book)
            {
                Initialize(book);
                _name = IndexForInspector.ToString();
            }

            public Node()
            {
                _name = "ROOT";
            }

            #region Inspector

            [NonSerialized] private pegi.CollectionInspectorMeta _collectionMeta;
            public int GetCount() => _childNodes.Count;

            [NonSerialized] private bool _showConfigs;
            private static string _inspectedService = "";

            private string CfgsCount => configs.Count == 0 ? "" :
                (" " + (configs.Count == 1 ? configs.GetElementAt(0).Key : configs.Count.ToString()));

            internal void TrySaveCfgAndRemoveFrom(List<ITaggedCfg> lstCopy) 
            {
                for (int i = lstCopy.Count - 1; i >= 0; i--)
                {
                    var s = lstCopy[i];
                    var tag = s.TagForConfig;

                    if (configs.ContainsKey(tag))
                    {
                        configs[tag] = s.Encode().CfgData;
                        lstCopy.Remove(s);
                    }
                }
            }

            public void Inspect(NodesChain myChain) 
            {
                if (_collectionMeta == null)
                    _collectionMeta = new pegi.CollectionInspectorMeta(_name, showAddButton: false, allowDeleting: false, showEditListButton: false);

                pegi.Nl();

                if ("Configurations {0}".F(CfgsCount).PegiLabel().IsConditionally_Entered(canEnter: Mgmt.IsCurrent(this), ref _showConfigs).Nl())
                {
                    List<ITaggedCfg> lst = Singleton.GetAll<ITaggedCfg>();

                    bool inspectingService = !_inspectedService.IsNullOrEmpty();

                    if (inspectingService && (Icon.Exit.Click() | "Exit {0}".F(_inspectedService).PegiLabel().ClickLabel()))
                        _inspectedService = "";

                    pegi.Nl();

                    var servicesChanged = pegi.ChangeTrackStart();

                    for  (int i=0; i<lst.Count; i++)
                    {
                        var s = lst[i];

                        var tag = s.TagForConfig;

                        if (inspectingService)
                        {
                            if (tag.Equals(_inspectedService))
                            {
                                pegi.Try_Nested_Inspect(s);
                            }
                        }
                        else
                        {
                            if (configs.ContainsKey(tag))
                            {
                                if (Icon.Delete.ClickConfirm("del" + tag))
                                    configs.Remove(tag);

                                if (Icon.Save.Click("Save Changes"))
                                    configs[tag] = s.Encode().CfgData;
                            }
                            else
                            {
                                if (Icon.SaveAsNew.Click("Create Settings Override for this node"))
                                    configs[tag] = s.Encode().CfgData;
                            }

                            // TODO: Fallback to parent nodes to save changes
                            if (Icon.Enter.Click() | s.GetNameForInspector().PegiLabel().ClickLabel())
                                _inspectedService = s.TagForConfig;

                            (s as UnityEngine.Object).ClickHighlight();
                        }

                        pegi.Nl();
                    }

                    if (servicesChanged)
                        Mgmt.CurrentChain.SaveConfigsOfServicesToChain();
                }

                if (!_showConfigs)
                {
                    _collectionMeta.Label = _name;
                    _collectionMeta.Edit_List(_childNodes).Nl();

                    if (_collectionMeta.IsAnyEntered == false && "Add Node".PegiLabel().Click().Nl())
                        _childNodes.Add(new Node(_chain_ForInspector.Book));
                }
            }

            public void Inspect()
            {
                using (InspectChainUse())
                {
                    Inspect(_chain_ForInspector);
                }
            }

            private void InspectSetCurrentOptions() 
            {
                if (Mgmt.AnyEntered == false)
                {
                    if (Icon.Play.Click("Enter this Node"))
                        Mgmt.SetCurrent(_chain_ForInspector);
                }
                else if (Mgmt.IsCurrent(this))
                {
                    if (Icon.Save.Click("Save Changes"))
                        Mgmt.CurrentChain.SaveConfigsOfServicesToChain();

                    if (_chain_ForInspector.Nodes.Count > 1)
                    {
                        if ("Save & To Prev".PegiLabel().Click())
                        {
                            if (Mgmt.AnyEntered)
                                Mgmt.CurrentChain.SaveConfigsOfServicesToChain();
                            Mgmt.SetCurrent(_chain_ForInspector.GetNodeInChain(_chain_ForInspector.Nodes.Count - 2));
                        }
                    }
                    else
                        Icon.Active.Draw();
                }
                else if (IsEntered)
                {
                    if ("Save & Back Here".PegiLabel().Click())
                    {
                        if (Mgmt.AnyEntered)
                            Mgmt.CurrentChain.SaveConfigsOfServicesToChain();

                        Mgmt.SetCurrent(_chain_ForInspector);
                    }
                }
                else if ((Mgmt.AnyEntered && "Save & Enter".PegiLabel(toolTip: "Save changes to {0}".F(Mgmt.CurrentChain.LastNode.GetNameForInspector())).Click()) || (!Mgmt.AnyEntered && "Enter".PegiLabel().Click()))
                {
                    if (Mgmt.AnyEntered)
                        Mgmt.CurrentChain.SaveConfigsOfServicesToChain();

                    Mgmt.SetCurrent(_chain_ForInspector);
                }
            }

            public void InspectInList(ref int edited, int ind)
            {

                using (InspectChainUse())
                {
                    if (Icon.Enter.Click())
                        edited = ind;

                    if ("ID {0}  ({1} br, {2} cfgs)".F(_index, GetCount(), CfgsCount).PegiLabel(width: 120).ClickLabel())
                        edited = ind;

                    pegi.inspect_Name(this);

                    InspectSetCurrentOptions();
                }
            }

            private IDisposable InspectChainUse() => _chain_ForInspector.AddAndUse(this);

            #endregion

            private class ConfigsDictionary : SerializableDictionary<string, CfgData> { }
        }
    }
}