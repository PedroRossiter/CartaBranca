using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Balanco senoidal simples, com deriva horizontal opcional.
    /// Usado nos naipes decorativos da tela de menu.</summary>
    public class Flutuar : MonoBehaviour
    {
        public float amplitude = 0.4f;
        public float frequencia = 1.2f;
        public float derivaX = 0f;
        public float limiteX = 16f;

        Vector3 _base;
        float _fase;

        void Start()
        {
            _base = transform.position;
            _fase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            _base.x += derivaX * Time.deltaTime;
            if (derivaX != 0f && Mathf.Abs(_base.x) > limiteX) _base.x = -Mathf.Sign(_base.x) * limiteX;
            transform.position = _base + Vector3.up * Mathf.Sin(Time.time * frequencia + _fase) * amplitude;
        }
    }
}
