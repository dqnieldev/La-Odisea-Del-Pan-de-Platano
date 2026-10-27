using UnityEngine;

namespace Odisea.Core
{
    /// <summary>Suena una vez al aparecer. Para efectos que se crean al momento (el embarrón de la bolita de masa).</summary>
    public class SoundOnStart : MonoBehaviour
    {
        [SerializeField] private SoundEffect sound = new();

        private void Start() => sound.Play();
    }
}
