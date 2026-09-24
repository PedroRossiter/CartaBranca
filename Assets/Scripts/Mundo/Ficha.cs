using UnityEngine;
using CartaBranca.Nucleo;

namespace CartaBranca.Mundo
{
    /// <summary>Ficha de cassino solta por um naipe derrubado: o coletavel do sistema de recompensas.
    /// Salta, cai ate um piso, gira e SOME depois de alguns segundos (piscando no fim):
    /// a recompensa existe, mas o jogador precisa se arriscar para buscar.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Ficha : MonoBehaviour
    {
        public int valor = 1;
        public Sprite[] quadros;
        public GameObject brilhoPrefab;

        [SerializeField] float duracao = 8f;
        [SerializeField] float gravidade = 30f;
        [SerializeField] float raioIma = 1.8f;
        [SerializeField] float velocidadeIma = 14f;
        [SerializeField] float alturaSobreOChao = 0.3f;

        SpriteRenderer _sr;
        Transform _jogador;
        Vector2 _velocidade;
        float _nasceu;
        float _fase;
        bool _pousada;
        bool _coletada;
        ContactFilter2D _filtro;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        public float TempoRestante { get { return Mathf.Max(0f, duracao - (Time.time - _nasceu)); } }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _nasceu = Time.time;
            _fase = Random.Range(0f, 4f);
            _filtro = new ContactFilter2D();
            _filtro.useTriggers = false;
            _filtro.useLayerMask = false;
        }

        void Start()
        {
            GameObject j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) _jogador = j.transform;
            if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.AoReiniciar += Sumir;
        }

        void OnDestroy()
        {
            if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.AoReiniciar -= Sumir;
        }

        public void Lancar(Vector2 velocidade)
        {
            _velocidade = velocidade;
            _pousada = false;
        }

        void Update()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo != null && jogo.EmPausa) return;

            float dt = Time.deltaTime;
            float resta = TempoRestante;
            if (resta <= 0f) { Destroy(gameObject); return; }

            // gira e pisca nos ultimos 2 segundos (cada vez mais rapido)
            if (quadros != null && quadros.Length > 0)
                _sr.sprite = quadros[(int)((Time.time + _fase) * 10f) % quadros.Length];
            _sr.enabled = resta > 2f || Mathf.Repeat(Time.time * (resta < 1f ? 16f : 8f), 1f) > 0.35f;

            Vector2 pos = transform.position;

            // ima: perto da Carta Branca, a ficha vem sozinha
            if (_jogador != null && Time.time - _nasceu > 0.3f)
            {
                Vector2 alvo = _jogador.position;
                if (Vector2.Distance(pos, alvo) < raioIma)
                {
                    transform.position = Vector2.MoveTowards(pos, alvo, velocidadeIma * dt);
                    return;
                }
            }

            if (_pousada) return;

            _velocidade.y -= gravidade * dt;
            _velocidade.x = Mathf.MoveTowards(_velocidade.x, 0f, 3f * dt);
            Vector2 passo = _velocidade * dt;

            if (_velocidade.y <= 0f)
            {
                int n = Physics2D.Raycast(pos, Vector2.down, _filtro, _hits, -passo.y + alturaSobreOChao);
                for (int i = 0; i < n; i++)
                {
                    if (_hits[i].collider == null || _hits[i].collider.isTrigger) continue;
                    transform.position = new Vector3(pos.x, _hits[i].point.y + alturaSobreOChao, 0f);
                    _pousada = true;
                    return;
                }
            }

            pos += passo;
            pos.x = Mathf.Clamp(pos.x, -23.5f, 23.5f);
            transform.position = new Vector3(pos.x, pos.y, 0f);
            if (pos.y < -24f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D outro)
        {
            if (_coletada || !outro.CompareTag("Player")) return;
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null || jogo.EmPausa) return;

            _coletada = true;
            jogo.ColetarFicha(valor);
            Sonoplasta.Tocar(Som.Ficha, valor > 1 ? 1f : 0.7f);
            if (brilhoPrefab != null) Instantiate(brilhoPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        void Sumir()
        {
            Destroy(gameObject);
        }
    }
}
