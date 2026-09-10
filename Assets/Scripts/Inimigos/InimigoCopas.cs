using UnityEngine;

namespace CartaBranca.Inimigos
{
    /// <summary>Naipe de Copas: voa, gira em volta da Carta Branca e mergulha de tempos
    /// em tempos. Sobrescreve Configurar para acelerar tambem o intervalo do mergulho.</summary>
    public class InimigoCopas : Inimigo
    {
        [Header("Copas")]
        [SerializeField] float amplitude = 1.1f;
        [SerializeField] float frequencia = 2.4f;
        [SerializeField] float alturaDeVoo = 3.2f;
        [SerializeField] float intervaloMergulho = 3.4f;
        [SerializeField] float velocidadeMergulho = 12f;
        [SerializeField] float duracaoMergulho = 0.85f;

        float _fase;
        float _proximoMergulho;
        float _fimMergulho;
        Vector2 _direcaoMergulho;

        protected override void Awake()
        {
            base.Awake();
            _fase = Random.Range(0f, Mathf.PI * 2f);
            _proximoMergulho = Time.time + Random.Range(1.5f, intervaloMergulho);
        }

        public override void Configurar(float escala, int vida)
        {
            base.Configurar(escala, vida);
            intervaloMergulho = Mathf.Max(1.2f, 3.4f / Mathf.Clamp(escala, 1f, 2.4f));
        }

        protected override void Comportamento()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;

            if (Time.time < _fimMergulho)
            {
                pos += (Vector3)(_direcaoMergulho * velocidadeMergulho * dt);
                transform.position = pos;
                if (pos.y < -24f) Retirar();
                return;
            }

            if (Alvo != null && Alvo.Vivo)
            {
                Vector3 destino = Alvo.transform.position + Vector3.up * alturaDeVoo;
                pos = Vector3.MoveTowards(pos, destino, Velocidade * dt);

                if (Time.time >= _proximoMergulho)
                {
                    _proximoMergulho = Time.time + intervaloMergulho + Random.Range(0f, 1f);
                    _fimMergulho = Time.time + duracaoMergulho;
                    _direcaoMergulho = ((Vector2)(Alvo.transform.position - pos)).normalized;
                    return;
                }
            }
            else
            {
                pos += Vector3.left * Velocidade * dt;
            }

            _fase += frequencia * dt;
            pos.y += Mathf.Sin(_fase) * amplitude * dt;
            transform.position = pos;

            if (Alvo != null)
            {
                Vector3 e = transform.localScale;
                e.x = Mathf.Abs(e.x) * (Alvo.transform.position.x >= pos.x ? 1 : -1);
                transform.localScale = e;
            }
        }
    }
}
