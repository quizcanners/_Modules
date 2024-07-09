using QuizCanners.Inspect;
using QuizCanners.Utils;
using TMPro;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class C_CurrencyAnimatedText : MonoBehaviour, IPEGI, INeedAttention
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private SO_CurrencyAnimationPrototype _currency;

        private readonly Gate.Double _valueGate = new();

        private void LateUpdate()
        {
            Singleton.Try<Pool_CurrencyAnimationController>(s => 
            {
                var val = s.GetAnimatedValue(_currency);
                if (_valueGate.TryChange(val))
                    _text.text = val.ToReadableString();
            });
        }

        private void Reset()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        #region Inspector
        void IPEGI.Inspect()
        {
            pegi.Nl();
            "Text".PegiLabel().Edit_IfNull(ref _text, gameObject).Nl();
            "Curence".PegiLabel().Edit(ref _currency).Nl();
        }

        public string NeedAttention()
        {
            if (!_text)
                return "Text not assigned";

            if (!_currency)
                return "Currency not assigned";

            return null;
        }
        #endregion

    }

    [PEGI_Inspector_Override(typeof(C_CurrencyAnimatedText))] 
    internal class C_CurrencyAnimatedTextDrawer : PEGI_Inspector_Override { }
}