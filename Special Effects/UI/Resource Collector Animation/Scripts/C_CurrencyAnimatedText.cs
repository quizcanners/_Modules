using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace QuizCanners.SpecialEffects
{
    public class C_CurrencyAnimatedText : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private SO_CurrencyAnimationPrototype _currency;

        private Gate.Double _valueGate = new Gate.Double();


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
            _text = GetComponent<Text>();
        }

    }
}