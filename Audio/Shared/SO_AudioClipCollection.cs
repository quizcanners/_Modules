using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + Singleton_GameController.PROJECT_NAME + "/Audio/" + FILE_NAME)]
    public class SO_AudioClipCollection : ScriptableObject
    {
        private List<AudioClip> _clips;
        public float Volume = 1;
        [NonSerialized] private int _previous = -1;

        public AudioClip GetRandom() => _clips.GetRandom(ref _previous);

        public const string FILE_NAME = "Sound Clips Collection";



    }
}