﻿using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;

namespace QuizCanners.IsItGame.Triggers.Dialogue
{

    internal class Interaction : ICfg, IPEGI, IGotReadOnlyName, ICondition, INeedAttention, IPEGI_ListInspect {

        private string referenceName = "";
        public ConditionBranch conditions = new();
        public ListOfSentences texts = new();
        public List<DialogueChoice> choices = new();
        public List<IntResult> finalResults = new();

        public void ResetSentences() {
            texts.Reset();
            foreach (var o in choices)
                o.ResetSentences();
        }

        public void Execute() {
            for (int j = 0; j < choices.Count; j++)
                if (choices[j].conditions.IsMet()) 
                { 
                    choices[j].results.Apply();
                    break;
                }

            finalResults.Apply();
        }

        public bool IsMet() => conditions.IsMet();

        #region Encode & Decode

        public CfgEncoder Encode() => new CfgEncoder()//this.EncodeUnrecognized()
            .Add_IfNotEmpty("ref", referenceName)
            .Add_IfNotDefault("Conds", conditions)
            .Add("txts", texts)
            .Add_IfNotEmpty("opt", choices)
            .Add_IfNotEmpty("fin", finalResults)
            .Add("it", _inspectedItems)
            .Add("bcm", _inspectedChoice)
            .Add("irm", _inspectedResult);

        public void DecodeTag(string tg, CfgData data) {
            switch (tg)  {
                case "ref": referenceName = data.ToString(); break;
                case "Conds": data.Decode(out conditions); break;
                case "txts": texts.Decode(data); break;
                case "opt": data.ToList(out choices); break;
                case "fin": data.ToList(out finalResults); break;
                case "it": _inspectedItems.Decode(data); break;
                case "bcm": _inspectedChoice.Decode(data); break;
                case "irm": _inspectedResult.Decode(data); break;
            }
        }
        #endregion

        #region Inspector

        public static List<Interaction> inspectedList = new();
        
        public void RenameReferenceName (string oldName, string newName) {
            foreach (var o in choices)
                o.RenameReference(oldName, newName);
        }

        private readonly pegi.CollectionInspectorMeta _inspectedChoice = new("Choices");
        private readonly pegi.CollectionInspectorMeta _inspectedResult = new("Results");
        public static bool renameLinkedReferences = true; 

        public string ReferenceName {
            get { return referenceName; }
            set
            {
                if (renameLinkedReferences && SO_Dialogue.inspected != null)
                    SO_Dialogue.inspected.interactionBranch.RenameReferance(referenceName, value);
                referenceName = value;
            }
        }

        /*
        public string NameForPEGI
        {
            get { return referenceName; }
            set {
                if (renameLinkedReferences && DialogueNode.inspected != null)
                    DialogueNode.inspected.interactionBranch.RenameReferance(referenceName, value);
                referenceName = value;
            }
        }*/
        
        public string GetReadOnlyName() => texts.NameForInspector;

        public string NeedAttention() {

            string na = pegi.NeedsAttention(choices);

            return na;
        }

        [UnityEngine.SerializeField]  private pegi.EnterExitContext _inspectedItems = new();

        public void Inspect() {
            using (_inspectedItems.StartContext())
            {
                if (_inspectedItems.IsAnyEntered == false)
                {
                    var n = ReferenceName;

                    if (n.IsNullOrEmpty() && "Add Reference name".PegiLabel().Click())
                        ReferenceName = "Rename Me";

                    if (!n.IsNullOrEmpty())
                    {
                        if (renameLinkedReferences)
                        {
                            if ("Ref".PegiLabel(50).EditDelayed(ref n))
                                ReferenceName = n;
                        }
                        else if ("Ref".PegiLabel(50).Edit(ref n))
                            ReferenceName = n;

                        pegi.Toggle(ref renameLinkedReferences, Icon.Link, Icon.UnLinked,
                            "Will all the references to this Interaction be renamed as well.");
                    }

                    pegi.FullWindow.DocumentationClickOpen(() =>
                            "You can use reference to link end of one interaction with the start of another. But the first text of it will be skipped. First sentence is the option user picks to start an interaction. Like 'Lets talk about ...' " +
                             "which is not needed if the subject is currently being discussed from interaction that came to an end.", "About option referance"
                            );

                    pegi.Nl();

                    MultilanguageSentence.LanguageSelector_PEGI(); pegi.Nl();
                }

              

                conditions.Enter_Inspect_AsList().Nl();

                "Texts".PegiLabel().Enter_Inspect(texts).Nl();

                _inspectedChoice.Enter_List(choices).Nl(); // _ifNotEntered();

                int cnt = finalResults.Count;

                if (_inspectedResult.Enter_List(finalResults))
                {
                    // if (finalResults.Count>cnt)
                    // finalResults[finalResults.Count-1].SetLastUsedTrigger();
                }

                if (_inspectedItems.IsAnyEntered == false)
                    pegi.FullWindow.DocumentationClickOpen("Results that will be set the moment any choice is picked, before the text that goes after it", "About Final Results");

                pegi.Nl();

            }
        }

        public void InspectInList(ref int edited, int ind)
        {

            texts.inspect_Name();

            if (Icon.Enter.Click())
                edited = ind;
        }
        
        #endregion

    }

    internal class InteractionBranch : LogicBranch<Interaction>  {
        public override string NameForElements => "Interactions";

        private void RenameReferenceLoop(LogicBranch<Interaction> br, string oldName, string newName) {
            foreach (var e in br.elements)
                e.RenameReferenceName(oldName, newName);

            foreach (var sb in br.subBranches)
                RenameReferenceLoop(sb, oldName, newName);
        }
        
       public override void Inspect()
        {
            Interaction.inspectedList.Clear();

            CollectAll(ref Interaction.inspectedList);

            base.Inspect();
        }

        public void RenameReferance (string oldName, string newName) => RenameReferenceLoop(this, oldName, newName);
        
        public InteractionBranch() 
        {
            name = "root";
        }
    }

    internal class DialogueChoice : ICfg, IPEGI, IGotName, INeedAttention
    {
        public ConditionBranch conditions = new();
        public MultilanguageSentence text = new();
        public ListOfSentences text2 = new();
        public List<IntResult> results = new();
        public string nextOne = "";

        public void ResetSentences() {
            text.Reset();
            text2.Reset();
        }

        #region Encode & Decode


        public CfgEncoder Encode() => new CfgEncoder()//this.EncodeUnrecognized()
         .Add_IfNotEmpty("goto", nextOne)
         .Add("cnd", conditions)
         .Add("t", text)
         .Add("ts2b", text2)
         .Add_IfNotEmpty("res", results)
         .Add("ins", _inspectedItems);

        public void DecodeTag(string tg, CfgData data)
        {
            switch (tg)
            {
                case "goto": nextOne = data.ToString(); break;
                case "cnd": data.Decode(out conditions); break;
                case "t": text.Decode(data); break;
                case "ts2b": text2.Decode(data); break;
                case "res": data.ToList(out results); break;
                case "ins": _inspectedItems.Decode(data); break;
            }
        }
        #endregion

        #region Inspector

        public void RenameReference(string oldName, string newName) => nextOne = nextOne.SameAs(oldName) ? newName : nextOne;

        private int inspectedResult = -1;
        [UnityEngine.SerializeField]  private pegi.EnterExitContext _inspectedItems = new();

        public string NeedAttention() {

            var na = text.NeedAttention();
            if (na != null) return na;
            
            return null;
        }

        public string NameForInspector {
            get { return text.NameForInspector; }
            set { text.NameForInspector = value; } }

        public void Inspect() {

            using (_inspectedItems.StartContext())
            {
                conditions.Enter_Inspect_AsList().Nl();

                if (text.GetNameForInspector().PegiLabel().IsEntered())
                    text.Nested_Inspect();
                else if (!_inspectedItems.IsAnyEntered)
                    pegi.Nested_Inspect(MultilanguageSentence.LanguageSelector_PEGI).Nl();

                "Results".PegiLabel().Enter_List(results, ref inspectedResult).Nl();

                if (_inspectedItems.IsCurrentEntered)
                    MultilanguageSentence.LanguageSelector_PEGI(); pegi.Nl();

                "After choice texts".PegiLabel().Enter_Inspect(text2).Nl();

                if (_inspectedItems.IsAnyEntered == false)
                {
                    if (!nextOne.IsNullOrEmpty() && Icon.Delete.Click("Remove any followups"))
                        nextOne = "";

                    if (nextOne.IsNullOrEmpty())
                    {
                        if ("Go To".PegiLabel().Click())
                            nextOne = "UNSET";
                    }
                    else
                        "Go To".PegiLabel(60).Select_iGotDisplayName(ref nextOne, Interaction.inspectedList).Nl();
                }
            }
        }
        
        #endregion
    }


}
