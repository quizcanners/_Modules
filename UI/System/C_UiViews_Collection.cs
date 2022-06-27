using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class C_UiViews_Collection : MonoBehaviour, IPEGI
    {
        internal static List<C_UiViews_Collection> _uiViews = new();


        [SerializeField] private ViewsDic _views = new();
        [SerializeField] private List<UI_TypedView> _typedViews = new();


        public bool TryGet(Game.Enums.View view, out GameObject result)
        {
            if (_views.TryGetValue(view, out result) && result)
                return true;

            foreach (var tv in _typedViews) 
            {
                if (tv && tv.MyView == view)
                {
                    result = tv.gameObject;
                    return true;
                }
            }

            return false;
        }
           
        public void Inspect()
        {
            _views.Nested_Inspect();

            "Key".PegiLabel(30).Edit_Enum(ref tmpKey);

            "Value".PegiLabel(40).Edit(ref tmpGo);

            if (tmpGo && !_views.ContainsKey(tmpKey) && Icon.Add.Click("Add element"))
                _views.Add(tmpKey, tmpGo);

            pegi.Nl();


            "Typed Views".PegiLabel().Edit_List_UObj(_typedViews).Nl();


        }

        [Serializable]
        private class ViewsDic : SerializableDictionary<Game.Enums.View, GameObject>{}

        [NonSerialized] Game.Enums.View tmpKey;
        [NonSerialized] GameObject tmpGo;

        void OnEnable() 
        {
            _uiViews.Add(this);
        }

        void OnDisable() 
        {
            _uiViews.Remove(this);
        }
    }

    [PEGI_Inspector_Override(typeof(C_UiViews_Collection))] internal class C_UiViews_CollectionDrawer : PEGI_Inspector_Override { }
}
