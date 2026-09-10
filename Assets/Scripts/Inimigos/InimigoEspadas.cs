using UnityEngine;

namespace CartaBranca.Inimigos
{
    /// <summary>Naipe de Espadas: capanga de chao. Persegue a Carta Branca andando,
    /// cai por gravidade propria e da pulinhos quando encosta em parede.</summary>
    public class InimigoEspadas : Inimigo
    {
        [Header("Espadas")]
        [SerializeField] float gravidade = 42f;
        [SerializeField] float alturaPes = 0.62f;
        [SerializeField] float forcaPulo = 12f;
        [SerializeField] float alcancePerseguicao = 26f;

        float _vy;
        int _direcao = -1;
        float _proximoPulo;
        ContactFilter2D _filtro;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        protected override void Awake()
        {
            base.Awake();
            _filtro = new ContactFilter2D();
            _filtro.useTriggers = false;
            _filtro.useLayerMask = false;
            _proximoPulo = Time.time + Random.Range(1f, 2.5f);
        }

        protected override void Comportamento()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;

            if (Alvo != null && Alvo.Vivo)
            {
                float dx = Alvo.transform.position.x - pos.x;
                if (Mathf.Abs(dx) < alcancePerseguicao && Mathf.Abs(dx) > 0.35f)
                    _direcao = dx > 0f ? 1 : -1;
            }

            // gravidade manual (corpo cinematico: controle total, zero surpresa da fisica)
            _vy -= gravidade * dt;
            pos.y += _vy * dt;
            pos.x += _direcao * Velocidade * dt;

            // apoio: raio para baixo procurando chao solido
            int n = Physics2D.Raycast(new Vector2(pos.x, pos.y + 0.4f), Vector2.down, _filtro, _hits, 1.4f);
            for (int i = 0; i < n; i++)
            {
                if (_hits[i].collider == null || _hits[i].collider.isTrigger) continue;
                float topo = _hits[i].point.y + alturaPes;
                if (_vy <= 0f && pos.y <= topo + 0.05f)
                {
                    pos.y = topo;
                    _vy = 0f;
                }
                break;
            }

            // parede na frente: pula por cima
            int m = Physics2D.Raycast(new Vector2(pos.x, pos.y), new Vector2(_direcao, 0f), _filtro, _hits, 0.7f);
            bool bloqueado = false;
            for (int i = 0; i < m; i++)
            {
                if (_hits[i].collider != null && !_hits[i].collider.isTrigger) { bloqueado = true; break; }
            }
            if ((bloqueado || Time.time >= _proximoPulo) && Mathf.Approximately(_vy, 0f))
            {
                _vy = forcaPulo;
                _proximoPulo = Time.time + Random.Range(1.8f, 3.6f);
            }

            transform.position = pos;

            Vector3 e = transform.localScale;
            e.x = Mathf.Abs(e.x) * _direcao;
            transform.localScale = e;

            if (pos.y < -24f) Retirar();
        }
    }
}
