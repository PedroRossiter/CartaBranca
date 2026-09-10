using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Particula pobre: cresce, sobe e some. Usada em mortes e impactos.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Efeito : MonoBehaviour
    {
        public float duracao = 0.35f;
        public float crescimento = 2.4f;
        public float subida = 1.2f;

        SpriteRenderer _sr;
        float _nasceu;
        Vector3 _escalaInicial;
        Color _cor;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _escalaInicial = transform.localScale;
            _cor = _sr.color;
        }

        void OnEnable()
        {
            _nasceu = Time.time;
        }

        void Update()
        {
            float t = (Time.time - _nasceu) / Mathf.Max(0.01f, duracao);
            if (t >= 1f) { Destroy(gameObject); return; }

            transform.localScale = _escalaInicial * (1f + crescimento * t);
            transform.position += Vector3.up * subida * Time.deltaTime;
            _sr.color = new Color(_cor.r, _cor.g, _cor.b, _cor.a * (1f - t));
        }

        public void Tingir(Color cor)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _cor = cor;
            _sr.color = cor;
        }
    }
}
