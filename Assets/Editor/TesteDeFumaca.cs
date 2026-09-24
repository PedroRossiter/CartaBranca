using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Jogador;
using CartaBranca.Inimigos;
using CartaBranca.Mundo;
using CartaBranca.UI;

namespace CartaBranca.EditorTools
{
    /// <summary>Teste de fumaca automatico: abre a arena, entra em Play e roteiriza uma partida
    /// (IA, fichas, Ultima Mao, fuga do Ouros, pausa, morte, reinicio e menu), conferindo cada regra
    /// e tirando fotos da tela em Build/Fotos. Resultado em Build/teste_fumaca.txt.
    /// Terminal: Unity.exe -batchmode -projectPath . -executeMethod CartaBranca.EditorTools.TesteDeFumaca.Rodar</summary>
    public static class TesteDeFumaca
    {
        const string PASTA_FOTOS = "Build/Fotos";
        const string RELATORIO = "Build/teste_fumaca.txt";

        static readonly List<string> Linhas = new List<string>();
        static int _falhas;
        static int _erros;
        static int _etapa;
        static double _t0;
        static bool _opcoesAntes;
        static EnterPlayModeOptions _modoAntes;

        static InimigoPaus _paus;
        static InimigoOuros _ouros;
        static Inimigo _vizinho;
        static float _distanciaInicialPaus;
        static Vector3 _posicaoInicialOuros;
        static int _fichasAntes;

        [MenuItem("Carta Branca/Teste de fumaça (Play automático)", false, 40)]
        public static void Rodar()
        {
            Linhas.Clear();
            _falhas = 0; _erros = 0; _etapa = 0;
            EditorSceneManager.OpenScene(ConstrutorDaArena.CENA);

            // sem recarregar o dominio: os campos estaticos deste teste sobrevivem a entrada no Play
            _opcoesAntes = EditorSettings.enterPlayModeOptionsEnabled;
            _modoAntes = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += Log;
            EditorApplication.update += Passo;
            EditorApplication.EnterPlaymode();
        }

        static void Log(string msg, string pilha, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                _erros++;
                Linhas.Add("  ERRO NO CONSOLE: " + msg);
            }
        }

        static void Checar(bool ok, string oque)
        {
            Linhas.Add((ok ? "  [ok]    " : "  [FALHA] ") + oque);
            if (!ok) _falhas++;
        }

        static void Nota(string texto)
        {
            Linhas.Add("  - " + texto);
        }

        static void Passo()
        {
            if (!EditorApplication.isPlaying) return;
            if (_etapa == 0) { _t0 = EditorApplication.timeSinceStartup; _etapa = 1; return; }
            double t = EditorApplication.timeSinceStartup - _t0;
            if (t > 90) { Checar(false, "tempo limite do teste"); Terminar(); return; }
            try { Etapas(t); }
            catch (System.Exception e) { Checar(false, "excecao no roteiro: " + e); Terminar(); }
        }

        static T Criar<T>(GameObject prefab, Vector3 pos) where T : Inimigo
        {
            GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
            T inimigo = go.GetComponent<T>();
            inimigo.Configurar(1f, 1);
            return inimigo;
        }

        static void Etapas(double t)
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            ControleCartaBranca jog = Object.FindAnyObjectByType<ControleCartaBranca>();
            GeradorDeInimigos ger = Object.FindAnyObjectByType<GeradorDeInimigos>();
            TelasDaPartida telas = Object.FindAnyObjectByType<TelasDaPartida>();

            switch (_etapa)
            {
                case 1:
                {
                    if (t < 1.5) return;
                    Linhas.Add("ARENA");
                    Checar(jogo != null && jogo.Estado == EstadoDaPartida.Jogando, "partida começa em Jogando, onda " + (jogo != null ? jogo.Onda : 0));
                    jog.TornarInvulneravel(14f);

                    MapaDeNavegacao mapa = MapaDeNavegacao.Instancia;
                    List<Vector2> rota = new List<Vector2>();
                    Checar(mapa != null && mapa.Caminho(new Vector2(-20f, 8f), new Vector2(0f, -5f), rota), "A* acha rota do céu ao chão");
                    Nota("rota com " + rota.Count + " nós; células livres na grade: " + mapa.CelulasLivres);
                    Checar(!mapa.Livre(new Vector2(-18.5f, -3f)), "plataforma da Tilemap bloqueia a grade");
                    bool contorna = mapa.Caminho(new Vector2(-18.5f, -4.6f), new Vector2(-18.5f, -1.4f), rota);
                    Checar(contorna && rota.Count > 8, "rota de baixo para cima da plataforma contorna a borda (" + rota.Count + " nós)");

                    _paus = Criar<InimigoPaus>(ger.pausPrefab, new Vector3(-19f, 6f, 0f));
                    _ouros = Criar<InimigoOuros>(ger.ourosPrefab, new Vector3(18f, 6f, 0f));   // longe: fora do raio da Ultima Mao
                    _distanciaInicialPaus = Vector2.Distance(_paus.transform.position, jog.transform.position);
                    _posicaoInicialOuros = _ouros.transform.position;
                    InimigoNavegante.MostrarRotas = true;
                    _etapa = 2;
                    break;
                }
                case 2:
                {
                    if (t < 4.5) return;
                    Foto("01_arena_ia");
                    Checar(_paus != null && Vector2.Distance(_paus.transform.position, jog.transform.position) < _distanciaInicialPaus - 3f,
                           "Paus se aproxima seguindo a rota");
                    Checar(_ouros != null && Vector3.Distance(_ouros.transform.position, _posicaoInicialOuros) > 1f, "Ouros se desloca (foge)");

                    Inimigo vitima = Criar<InimigoEspadas>(ger.espadasPrefab, jog.transform.position + new Vector3(-6f, 1f, 0f));
                    vitima.Danificar(99, vitima.transform.position);
                    _etapa = 3;
                    break;
                }
                case 3:
                {
                    if (t < 5.3) return;
                    int noChao = Object.FindObjectsByType<Ficha>().Length;
                    Checar(noChao > 0, "abate a carta solta fichas (" + noChao + " no chão)");
                    Checar(jogo.Sequencia >= 1, "abate abre a sequência (" + jogo.Sequencia + ")");

                    jogo.ColetarFicha(40);
                    _fichasAntes = jogo.Fichas;
                    _vizinho = Criar<InimigoEspadas>(ger.espadasPrefab, jog.transform.position + new Vector3(3f, 0.5f, 0f));
                    _etapa = 4;
                    break;
                }
                case 4:
                {
                    if (t < 5.8) return;
                    UltimaMao um = jog.GetComponent<UltimaMao>();
                    int abatesAntes = jogo.Abates;
                    um.Usar();
                    Linhas.Add("ÚLTIMA MÃO");
                    Checar(jogo.Fichas == _fichasAntes - 10, "custa 10 fichas (" + _fichasAntes + " -> " + jogo.Fichas + ")");
                    Checar(um.Custo == 15, "o próximo uso custa 15");
                    Checar(jogo.Abates > abatesAntes, "derrubou " + (jogo.Abates - abatesAntes) + " naipe(s) no raio");
                    _etapa = 5;
                    break;
                }
                case 5:
                {
                    if (t < 5.95) return;
                    Foto("02_ultima_mao");
                    Checar(_vizinho == null, "o vizinho saiu de cena");
                    _etapa = 6;
                    break;
                }
                case 6:
                {
                    if (t < 12.0) return;
                    Linhas.Add("NAIPE DE OUROS");
                    Checar(jogo.FichasPerdidas > 0, "Ouros fugiu e a casa cobrou " + jogo.FichasPerdidas + " fichas");
                    Checar(ger.OurosAtivo == null || ger.OurosAtivo.Fugindo, "Ouros deixou a mesa");
                    jogo.Pausar(true);
                    _etapa = 7;
                    break;
                }
                case 7:
                {
                    if (t < 12.4) return;
                    Linhas.Add("PAUSA");
                    Checar(Mathf.Approximately(Time.timeScale, 0f), "Time.timeScale = 0");
                    Checar(telas != null && telas.painelPausa.activeSelf, "painel de pausa aberto");
                    Foto("03_pausa");
                    jogo.Pausar(false);
                    Checar(Mathf.Approximately(Time.timeScale, 1f) && !telas.painelPausa.activeSelf, "despausa");
                    _etapa = 8;
                    break;
                }
                case 8:
                {
                    if (t < 16.5) return;
                    Linhas.Add("MORTE E FIM DE JOGO");
                    jog.Danificar(99, jog.transform.position);
                    Checar(!jog.Vivo, "um toque mata a Carta Branca");
                    _etapa = 9;
                    break;
                }
                case 9:
                {
                    if (t < 19.5) return;
                    Checar(jogo.Estado == EstadoDaPartida.FimDeJogo, "entra em FimDeJogo");
                    Checar(telas != null && telas.painelFim.activeSelf, "painel de fim aberto");
                    Nota("placar final " + jogo.PontuacaoFinal + " (pontos " + jogo.Pontos + " + fichas " + jogo.Fichas + ")");
                    Foto("04_fim_de_jogo");
                    jogo.Reiniciar();
                    _etapa = 10;
                    break;
                }
                case 10:
                {
                    if (t < 20.5) return;
                    Checar(jogo.Onda == 1 && jogo.Pontos == 0 && jogo.Fichas == 0 && jogo.Abates == 0 && jog.Vivo,
                           "reinício volta tudo ao estado inicial (onda, pontos, fichas, vida)");
                    Checar(!telas.painelFim.activeSelf, "painel de fim fechado");
                    Checar(Object.FindObjectsByType<Ficha>().Length == 0, "fichas limpas da mesa");
                    jogo.IrParaMenu();
                    _etapa = 11;
                    break;
                }
                case 11:
                {
                    if (t < 22.5) return;
                    Linhas.Add("MENU");
                    Checar(Object.FindAnyObjectByType<MenuPrincipal>() != null, "cena do menu carrega");
                    Foto("05_menu");
                    MenuPrincipal menu = Object.FindAnyObjectByType<MenuPrincipal>();
                    if (menu != null)
                    {
                        menu.painelComoJogar.SetActive(true);
                        menu.painelPrincipal.SetActive(false);
                        Foto("06_como_jogar");
                    }
                    Terminar();
                    break;
                }
            }
        }

        /// <summary>Renderiza a camera principal (com os Canvas temporariamente em modo camera) num PNG.</summary>
        static void Foto(string nome)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Directory.CreateDirectory(PASTA_FOTOS);

            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            RenderMode[] modos = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modos[i] = canvases[i].renderMode;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 1f;
                canvases[i].sortingOrder += 1000;   // UI por cima dos sprites, como no modo Overlay
            }

            const int L = 1280, A = 720;
            RenderTexture rt = new RenderTexture(L, A, 24);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(L, A, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, L, A), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(PASTA_FOTOS, nome + ".png"), tex.EncodeToPNG());

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modos[i];
                canvases[i].sortingOrder -= 1000;
            }
        }

        static void Terminar()
        {
            EditorApplication.update -= Passo;
            Application.logMessageReceived -= Log;
            InimigoNavegante.MostrarRotas = false;
            EditorSettings.enterPlayModeOptionsEnabled = _opcoesAntes;
            EditorSettings.enterPlayModeOptions = _modoAntes;

            Linhas.Insert(0, "TESTE DE FUMAÇA - Carta Branca DIU3   falhas: " + _falhas + "   erros no console: " + _erros);
            Directory.CreateDirectory("Build");
            File.WriteAllLines(RELATORIO, Linhas.ToArray());
            Debug.Log(string.Join("\n", Linhas.ToArray()));

            if (Application.isBatchMode) EditorApplication.Exit(_falhas + _erros == 0 ? 0 : 1);
            else EditorApplication.ExitPlaymode();
        }
    }
}
