using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Mundo;

namespace CartaBranca.Inimigos
{
    /// <summary>Naipe de Ouros: o pote da mesa. Entra a cada onda (a partir da 2), NAO machuca,
    /// e FOGE da Carta Branca usando a mesma IA de rota do Paus, trocando de esconderijo.
    /// Derrubado a cartas: solta um punhado de fichas de ouro (recompensa).
    /// Se o tempo acabar: escapa e a casa cobra parte do seu bolso (punicao).</summary>
    public class InimigoOuros : InimigoNavegante
    {
        [Header("Ouros")]
        [SerializeField] float tempoNaMesa = 9f;
        [SerializeField] float velocidadeDeFuga = 10f;
        [SerializeField] float trocaDeEsconderijo = 1.4f;
        [SerializeField] float distanciaDePanico = 7f;

        float _fogeEm;
        bool _fugindo;
        Vector2 _esconderijo;
        float _proximoEsconderijo;

        public float TempoRestante { get { return Mathf.Max(0f, _fogeEm - Time.time); } }
        public bool Fugindo { get { return _fugindo; } }

        protected override void Awake()
        {
            base.Awake();
            machucaAoTocar = false;
        }

        protected override void Start()
        {
            base.Start();
            _fogeEm = Time.time + tempoNaMesa;
            _esconderijo = transform.position;
        }

        /// <summary>Sobrescrita: o Ouros nao fica rapido demais (tem que dar para alcancar)
        /// e aguenta mais cartas que os outros.</summary>
        public override void Configurar(float escala, int vida)
        {
            base.Configurar(escala, vida);
            Velocidade = velocidadeBase * Mathf.Clamp(escala, 1f, 1.35f);
            DefinirVidaMaxima(vida + 2);
        }

        protected override Vector2 Destino()
        {
            MapaDeNavegacao mapa = MapaDeNavegacao.Instancia;
            if (mapa == null || Alvo == null) return _esconderijo;

            Vector2 ameaca = Alvo.transform.position;
            bool esconderijoQueimado = Vector2.Distance(_esconderijo, ameaca) < distanciaDePanico;
            if (Time.time >= _proximoEsconderijo || esconderijoQueimado)
            {
                _esconderijo = mapa.PontoDeFuga(transform.position, ameaca);
                _proximoEsconderijo = Time.time + trocaDeEsconderijo;
            }
            return _esconderijo;
        }

        protected override void Comportamento()
        {
            if (!_fugindo && Time.time >= _fogeEm)
            {
                _fugindo = true;
                TornarInvulneravel(99f);
                if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.OurosFugiu();
            }

            if (_fugindo)
            {
                transform.position += Vector3.up * velocidadeDeFuga * Time.deltaTime;
                if (transform.position.y > 16f) Retirar();
                return;
            }

            base.Comportamento();
        }

        protected override void AoLevarDano(Vector2 origem)
        {
            base.AoLevarDano(origem);
            // levou carta: entra em panico e troca de esconderijo na hora
            _proximoEsconderijo = 0f;
            ReplanejarJa();
        }
    }
}
