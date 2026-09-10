using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Camada de fundo que se move a uma fracao da camera.</summary>
    public class Paralaxe : MonoBehaviour
    {
        public float fator = 0.3f;
        public float alturaFixa = 0f;
        public bool travarAltura = true;

        Transform _cam;

        void Start()
        {
            if (Camera.main != null) _cam = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            Vector3 p = transform.position;
            p.x = _cam.position.x * fator;
            if (travarAltura) p.y = alturaFixa;
            transform.position = p;
        }
    }
}
