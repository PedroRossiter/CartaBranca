using UnityEngine;
using CartaBranca.Nucleo;

namespace CartaBranca.Jogador
{
    /// <summary>Carta Branca: quatro acoes (correr, pular, atirar carta, esquivar).
    /// Herda de Entidade e sobrescreve Morrer() para reiniciar a partida.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ControleCartaBranca : Entidade
    {
        [Header("Movimento")]
        [SerializeField] float velocidade = 7.4f;
        [SerializeField] float aceleracao = 60f;
        [SerializeField] float atrito = 45f;

        [Header("Pulo")]
        [SerializeField] float forcaPulo = 14.6f;
        [SerializeField] float gravidade = 3.6f;
        [SerializeField] float cortePulo = 0.45f;
        [SerializeField] float tempoCoiote = 0.12f;
        [SerializeField] float bufferPulo = 0.12f;

        [Header("Esquiva")]
        [SerializeField] float velocidadeEsquiva = 19f;
        [SerializeField] float duracaoEsquiva = 0.16f;
        [SerializeField] float recargaEsquiva = 0.85f;

        [Header("Tiro")]
        [SerializeField] float cadencia = 0.2f;
        [SerializeField] float recuo = 2.2f;

        [Header("Referencias (preenchidas pelo construtor)")]
        public Transform mao;
        public GameObject cartaPrefab;
        public GameObject impactoPrefab;
        public GameObject explosaoPrefab;
        public GameObject poeiraPrefab;
        public Animator animador;

        [Header("Chao")]
        [SerializeField] Vector2 caixaPes = new Vector2(0.72f, 0.14f);
        [SerializeField] float deslocamentoPes = -0.94f;

        Rigidbody2D _rb;
        Collider2D _colisor;
        Vector3 _nascimento;
        ContactFilter2D _filtroChao;
        readonly Collider2D[] _buffer = new Collider2D[6];

        float _entrada;
        float _ultimoChao;
        float _ultimoPedidoPulo = -99f;
        float _proximoTiro;
        float _fimEsquiva;
        float _proximaEsquiva;
        int _direcao = 1;
        bool _noChao;
        bool _morto;

        // Propriedades publicas para HUD e inimigos (information hiding).
        public bool Esquivando { get { return Time.time < _fimEsquiva; } }
        public float RecargaEsquiva
        {
            get { return Mathf.Clamp01(1f - Mathf.Max(0f, _proximaEsquiva - Time.time) / recargaEsquiva); }
        }
        public int Direcao { get { return _direcao; } }

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody2D>();
            _colisor = GetComponent<Collider2D>();
            _rb.gravityScale = gravidade;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _nascimento = transform.position;

            _filtroChao = new ContactFilter2D();
            _filtroChao.useTriggers = false;   // inimigos e cartas sao triggers: nao contam como chao
            _filtroChao.useLayerMask = false;
        }

        void Start()
        {
            if (GerenciadorDeJogo.Instancia != null)
            {
                GerenciadorDeJogo.Instancia.AoReiniciar += Renascer;
            }
        }

        void OnDestroy()
        {
            if (GerenciadorDeJogo.Instancia != null)
                GerenciadorDeJogo.Instancia.AoReiniciar -= Renascer;
        }

        protected override void Update()
        {
            base.Update();

            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (_morto || (jogo != null && jogo.EmPausa))
            {
                _entrada = 0f;
                return;
            }

            _entrada = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  _entrada -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) _entrada += 1f;

            if (Mathf.Abs(_entrada) > 0.01f) _direcao = _entrada > 0f ? 1 : -1;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _ultimoPedidoPulo = Time.time;

            if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
            {
                if (_rb.linearVelocity.y > 0f)
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * cortePulo);
            }

            if (Input.GetKey(KeyCode.J) || Input.GetMouseButton(0)) Atirar();

            if ((Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift)) && Time.time >= _proximaEsquiva)
                Esquivar();

            Animar();
        }

        void FixedUpdate()
        {
            if (_morto) return;

            int n = Physics2D.OverlapBox((Vector2)transform.position + Vector2.up * deslocamentoPes,
                                          caixaPes, 0f, _filtroChao, _buffer);
            _noChao = false;
            for (int i = 0; i < n; i++)
            {
                if (_buffer[i] != null && _buffer[i] != _colisor) { _noChao = true; break; }
            }
            if (_noChao) _ultimoChao = Time.time;

            Vector2 v = _rb.linearVelocity;

            if (Esquivando)
            {
                v.x = _direcao * velocidadeEsquiva;
                v.y = Mathf.Max(v.y, -1f);
            }
            else
            {
                float alvo = _entrada * velocidade;
                float taxa = Mathf.Abs(_entrada) > 0.01f ? aceleracao : atrito;
                v.x = Mathf.MoveTowards(v.x, alvo, taxa * Time.fixedDeltaTime);
            }

            bool podePular = (Time.time - _ultimoChao) <= tempoCoiote;
            bool pediu = (Time.time - _ultimoPedidoPulo) <= bufferPulo;
            if (podePular && pediu)
            {
                v.y = forcaPulo;
                _ultimoPedidoPulo = -99f;
                _ultimoChao = -99f;
                Soltar(poeiraPrefab, 0.6f);
            }

            v.y = Mathf.Max(v.y, -26f);
            _rb.linearVelocity = v;

            if (transform.position.y < -22f) Danificar(99, transform.position);
        }

        void Atirar()
        {
            if (Time.time < _proximoTiro || cartaPrefab == null) return;
            _proximoTiro = Time.time + cadencia;

            Vector3 origem = mao != null
                ? new Vector3(transform.position.x + _direcao * 0.85f, mao.position.y, 0f)
                : transform.position;

            GameObject go = Instantiate(cartaPrefab, origem, Quaternion.identity);
            Carta carta = go.GetComponent<Carta>();
            if (carta != null) carta.Lancar(new Vector2(_direcao, 0f), impactoPrefab);

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x - _direcao * recuo * 0.15f, _rb.linearVelocity.y);
            if (animador != null) animador.SetTrigger("Atirar");
        }

        void Esquivar()
        {
            _fimEsquiva = Time.time + duracaoEsquiva;
            _proximaEsquiva = Time.time + recargaEsquiva;
            TornarInvulneravel(duracaoEsquiva + 0.05f);
            Soltar(poeiraPrefab, 0.8f);
        }

        void Animar()
        {
            if (animador == null) return;
            animador.SetFloat("Velocidade", Mathf.Abs(_rb.linearVelocity.x));
            animador.SetFloat("VelY", _rb.linearVelocity.y);
            animador.SetBool("NoChao", _noChao);

            Vector3 e = transform.localScale;
            e.x = Mathf.Abs(e.x) * _direcao;
            transform.localScale = e;
        }

        void Soltar(GameObject prefab, float escala)
        {
            if (prefab == null) return;
            GameObject fx = Instantiate(prefab, transform.position + Vector3.down * 0.8f, Quaternion.identity);
            fx.transform.localScale *= escala;
        }

        protected override void AoLevarDano(Vector2 origem) { }

        /// <summary>Sobrescrita: a morte da Carta Branca zera a partida inteira.</summary>
        protected override void Morrer()
        {
            if (_morto) return;
            _morto = true;

            if (explosaoPrefab != null)
                Instantiate(explosaoPrefab, transform.position, Quaternion.identity);

            Corpo.enabled = false;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;

            if (GerenciadorDeJogo.Instancia != null)
                GerenciadorDeJogo.Instancia.MorteDoJogador();
        }

        /// <summary>Volta ao estado inicial: posicao, vida, fisica e sprite.</summary>
        public void Renascer()
        {
            _morto = false;
            RestaurarVida();
            transform.position = _nascimento;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
            _direcao = 1;
            _rb.simulated = true;
            _rb.linearVelocity = Vector2.zero;
            Corpo.enabled = true;
            TornarInvulneravel(1.2f);
            _fimEsquiva = 0f;
            _proximaEsquiva = 0f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * deslocamentoPes, caixaPes);
        }
    }
}
