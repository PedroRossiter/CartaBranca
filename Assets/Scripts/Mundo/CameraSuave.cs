using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Camera que segue a Carta Branca com folga e limites da arena,
    /// e com tres movimentos especiais:
    ///  - antecipacao: olha um pouco para onde o jogador esta correndo;
    ///  - tremor por "trauma" (ruido de Perlin), que decai sozinho;
    ///  - zoom dramatico temporario (Ultima Mao, morte).
    /// Roda em tempo real (unscaled) para continuar viva durante a camera lenta.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraSuave : MonoBehaviour
    {
        public static CameraSuave Instancia { get; private set; }

        public Transform alvo;
        public float suavidade = 6f;
        public Vector2 deslocamento = new Vector2(0f, 1.2f);
        public float limiteEsquerda = -18f;
        public float limiteDireita = 18f;
        public float limiteBaixo = -2f;
        public float limiteCima = 8f;

        [Header("Movimento especial")]
        public float antecipacao = 1.8f;
        public float tremorMaximo = 0.6f;
        public float decaimentoDoTremor = 1.5f;
        public float velocidadeDoZoom = 6f;

        Camera _cam;
        float _tamanhoBase;
        Vector3 _base;
        float _olhar;
        float _ultimoX;
        float _trauma;
        float _zoomAlvo;
        float _fimDoZoom;

        void Awake()
        {
            Instancia = this;
            _cam = GetComponent<Camera>();
            _tamanhoBase = _cam.orthographicSize;
            _base = transform.position;
            if (alvo != null) _ultimoX = alvo.position.x;
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        /// <summary>Soma trauma (0..1). O tremor e proporcional ao quadrado do trauma.</summary>
        public static void Tremer(float quantidade)
        {
            if (Instancia != null) Instancia._trauma = Mathf.Clamp01(Instancia._trauma + quantidade);
        }

        /// <summary>Aproxima (tamanho menor) ou afasta a camera por alguns segundos.</summary>
        public static void Aproximar(float tamanho, float duracao)
        {
            if (Instancia == null) return;
            Instancia._zoomAlvo = tamanho;
            Instancia._fimDoZoom = Time.unscaledTime + duracao;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            if (alvo != null)
            {
                // antecipacao pela velocidade horizontal real do alvo
                if (Time.deltaTime > 0f)
                {
                    float vx = (alvo.position.x - _ultimoX) / Time.deltaTime;
                    float desejado = Mathf.Clamp(vx / 7.4f, -1f, 1f) * antecipacao;
                    _olhar = Mathf.Lerp(_olhar, desejado, 1f - Mathf.Exp(-2.5f * dt));
                }
                _ultimoX = alvo.position.x;

                Vector3 destino = new Vector3(
                    Mathf.Clamp(alvo.position.x + deslocamento.x + _olhar, limiteEsquerda, limiteDireita),
                    Mathf.Clamp(alvo.position.y + deslocamento.y, limiteBaixo, limiteCima),
                    transform.position.z);
                _base = Vector3.Lerp(_base, destino, 1f - Mathf.Exp(-suavidade * dt));
            }

            // tremor
            Vector3 tremor = Vector3.zero;
            if (_trauma > 0f)
            {
                float forca = _trauma * _trauma * tremorMaximo;
                float t = Time.unscaledTime * 25f;
                tremor = new Vector3((Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * forca,
                                     (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * forca, 0f);
                _trauma = Mathf.Max(0f, _trauma - decaimentoDoTremor * dt);
            }
            transform.position = new Vector3(_base.x + tremor.x, _base.y + tremor.y, transform.position.z);

            // zoom
            float tamanho = Time.unscaledTime < _fimDoZoom ? _zoomAlvo : _tamanhoBase;
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, tamanho, 1f - Mathf.Exp(-velocidadeDoZoom * dt));
        }
    }
}
