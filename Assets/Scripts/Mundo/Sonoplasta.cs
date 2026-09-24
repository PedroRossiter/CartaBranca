using System.Collections.Generic;
using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Catalogo dos efeitos sonoros. O nome de cada item, em minusculas,
    /// e o nome do .wav em Assets/Audio (Som.Tiro -> tiro.wav).</summary>
    public enum Som
    {
        Tiro, Acerto, Abate, Ficha, Especial, Morte, Onda, Esquiva, Pulo, Clique, Cobra, Ouros, Negado
    }

    /// <summary>Toca os efeitos e a trilha. Um por cena, acessado de forma estatica
    /// para que qualquer script dispare um som com uma linha: Sonoplasta.Tocar(Som.Ficha).</summary>
    public class Sonoplasta : MonoBehaviour
    {
        public static Sonoplasta Instancia { get; private set; }

        [Tooltip("Indexado pelo enum Som (preenchido pelo construtor).")]
        public AudioClip[] clipes;
        public AudioClip musica;
        [Range(0f, 1f)] public float volumeEfeitos = 0.8f;
        [Range(0f, 1f)] public float volumeMusica = 0.32f;

        AudioSource _efeitos;
        AudioSource _trilha;
        // Map (Dictionary): ultimo instante em que cada som tocou, para nao "metralhar" o mesmo efeito
        readonly Dictionary<Som, float> _ultimoToque = new Dictionary<Som, float>();

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            _efeitos = gameObject.AddComponent<AudioSource>();
            _efeitos.playOnAwake = false;

            _trilha = gameObject.AddComponent<AudioSource>();
            _trilha.playOnAwake = false;
            _trilha.loop = true;
            _trilha.volume = volumeMusica;
            _trilha.clip = musica;
            if (musica != null) _trilha.Play();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public static void Tocar(Som som)
        {
            Tocar(som, 1f);
        }

        public static void Tocar(Som som, float volume)
        {
            if (Instancia != null) Instancia.TocarInterno(som, volume);
        }

        /// <summary>Abaixa a trilha (pausa, fim de jogo) ou devolve ao volume normal.</summary>
        public static void AbafarMusica(bool abafar)
        {
            if (Instancia == null || Instancia._trilha == null) return;
            Instancia._trilha.volume = Instancia.volumeMusica * (abafar ? 0.35f : 1f);
        }

        void TocarInterno(Som som, float volume)
        {
            int i = (int)som;
            if (clipes == null || i >= clipes.Length || clipes[i] == null) return;

            float ultimo;
            if (_ultimoToque.TryGetValue(som, out ultimo) && Time.unscaledTime - ultimo < 0.04f) return;
            _ultimoToque[som] = Time.unscaledTime;

            _efeitos.PlayOneShot(clipes[i], volume * volumeEfeitos);
        }
    }
}
