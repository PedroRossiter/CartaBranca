using UnityEngine;

namespace CartaBranca.Nucleo
{
    /// <summary>Persistencia simples do recorde. Tudo dentro de try/catch:
    /// PlayerPrefs pode falhar em plataformas com disco somente leitura.</summary>
    public static class Recordes
    {
        const string CHAVE_PONTOS = "cb_recorde_pontos";
        const string CHAVE_ONDA   = "cb_recorde_onda";

        public static int MelhorPontuacao
        {
            get
            {
                try { return PlayerPrefs.GetInt(CHAVE_PONTOS, 0); }
                catch (System.Exception e) { Debug.LogWarning("Recorde ilegivel: " + e.Message); return 0; }
            }
        }

        public static int MelhorOnda
        {
            get
            {
                try { return PlayerPrefs.GetInt(CHAVE_ONDA, 1); }
                catch (System.Exception e) { Debug.LogWarning("Recorde ilegivel: " + e.Message); return 1; }
            }
        }

        public static bool Salvar(int pontos, int onda)
        {
            bool novo = false;
            try
            {
                if (pontos > PlayerPrefs.GetInt(CHAVE_PONTOS, 0))
                {
                    PlayerPrefs.SetInt(CHAVE_PONTOS, pontos);
                    novo = true;
                }
                if (onda > PlayerPrefs.GetInt(CHAVE_ONDA, 1))
                {
                    PlayerPrefs.SetInt(CHAVE_ONDA, onda);
                }
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Nao foi possivel salvar o recorde: " + e.Message);
            }
            finally
            {
                // roda mesmo se der excecao: o jogo nunca trava por causa do recorde
            }
            return novo;
        }
    }
}
