using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Mantiene apagados a sus hijos (los enemigos de un nivel que corre a la derecha) hasta que la cámara llega cerca:
    /// así no caminan ni se caen al agua antes de que Ulises los vea. Cada hijo se enciende por su cuenta, al quedar a
    /// <see cref="margin"/> unidades de la orilla derecha de la pantalla.
    /// </summary>
    public class ActivateOnView : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float margin = 2f;

        private Transform[] pending;
        private Camera view;

        private void Awake()
        {
            pending = new Transform[transform.childCount];
            for (int i = 0; i < pending.Length; i++)
            {
                pending[i] = transform.GetChild(i);
                pending[i].gameObject.SetActive(false);
            }
            view = Camera.main;
        }

        private void Update()
        {
            if (view == null)
                return;
            float right = view.transform.position.x + view.orthographicSize * view.aspect + margin;
            for (int i = 0; i < pending.Length; i++)
            {
                if (pending[i] == null || pending[i].position.x > right)
                    continue;
                pending[i].gameObject.SetActive(true);
                pending[i] = null;
            }
        }
    }
}
