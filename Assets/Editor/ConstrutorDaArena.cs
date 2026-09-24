using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using CartaBranca.Nucleo;
using CartaBranca.Jogador;
using CartaBranca.Inimigos;
using CartaBranca.Mundo;
using CartaBranca.UI;

namespace CartaBranca.EditorTools
{
    /// <summary>Monta o jogo inteiro a partir dos PNGs, WAVs e scripts:
    /// importacao, clipes de animacao, controladores, tiles, prefabs, a cena do MENU e a cena da ARENA.
    /// Menu: Carta Branca > Construir jogo.  Build: Carta Branca > Gerar build WebGL.</summary>
    public static class ConstrutorDaArena
    {
        public const string ART   = "Assets/Art";
        public const string AUDIO = "Assets/Audio";
        public const string ANIM  = "Assets/Animacoes";
        public const string PREF  = "Assets/Prefabs";
        public const string TILES = "Assets/Tiles";
        public const string CENAS = "Assets/Cenas";
        public const string CENA  = CENAS + "/Arena.unity";
        public const string CENA_MENU = CENAS + "/Menu.unity";
        public const string PASTA_WEBGL = "Build/WebGL";
        const int PPU = 16;

        static readonly string[] REPETIR = { "ceu", "horizonte_longe", "horizonte_perto", "tile_chao", "tile_plataforma" };

        static readonly Color INK    = new Color32(11, 13, 20, 255);
        static readonly Color PAPEL  = new Color32(233, 231, 220, 255);
        static readonly Color CARMIM = new Color32(222, 70, 92, 255);
        static readonly Color OURO   = new Color32(198, 162, 88, 255);
        static readonly Color CINZA  = new Color32(150, 166, 196, 255);
        static readonly Color AZUL   = new Color32(66, 82, 118, 255);

        // ---------- geometria da arena ----------
        const float TOPO_CHAO = -6f;
        const float MEIA_LARGURA = 24f;

        // Plataformas em celulas da Tilemap (1 x 0,5 u): x inicial, x final (exclusivo), altura do centro.
        // Cada degrau fica a 2,5-3,25 u do anterior, dentro do alcance do pulo (4,34 u).
        static readonly float[,] PLATAFORMAS =
        {
            { -21f, -16f, -3f }, {  -7f, -2f, -3f }, {  8f, 13f, -3f },    // degrau baixo
            { -14f,  -8f,  0f }, {   0f,  6f,  0f }, { 14f, 19f,  0f },    // degrau medio
            { -16f, -12f, 2.5f }, {  5f, 10f, 2.5f },                       // degrau alto
            {  -4f,   1f,  5f }                                             // topo
        };

        [MenuItem("Carta Branca/Construir jogo (menu + arena)", false, 0)]
        public static void Construir()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Pasta(ANIM); Pasta(PREF); Pasta(CENAS); Pasta(TILES);
            ImportarTexturas();

            AnimatorController ctrlJogador = ControladorJogador();
            AnimatorController ctrlEspadas = ControladorSimples("Espadas", "esp_anda_", 4, 9f);
            AnimatorController ctrlCopas   = ControladorSimples("Copas", "cop_voa_", 4, 12f);
            AnimatorController ctrlPaus    = ControladorSimples("Paus", "paus_voa_", 4, 8f);
            AnimatorController ctrlOuros   = ControladorSimples("Ouros", "ouros_voa_", 4, 14f);
            AnimatorController ctrlCarta   = ControladorSimples("Carta", "carta_", 4, 20f);

            Prefabs p = new Prefabs();
            p.estouro   = PrefabEfeito("FxEstouro", "fx_estouro", 0.34f, 2.6f, 1.4f, 1f, PAPEL);
            p.poeira    = PrefabEfeito("FxPoeira", "fx_poeira", 0.40f, 1.9f, 0.6f, 0.7f, new Color(1f, 1f, 1f, 0.75f));
            p.anel      = PrefabEfeito("FxAnel", "fx_anel", 0.45f, 9f, 0f, 1f, PAPEL);
            p.brilho    = PrefabEfeito("FxBrilho", "fx_brilho", 0.30f, 1.4f, 1.2f, 1f, Color.white);
            p.ficha     = PrefabFicha("Ficha", "ficha_", 1, p.brilho);
            p.fichaOuro = PrefabFicha("FichaOuro", "fichaouro_", 5, p.brilho);
            p.carta     = PrefabCarta(ctrlCarta);
            p.espadas   = PrefabEspadas(ctrlEspadas, p);
            p.copas     = PrefabCopas(ctrlCopas, p);
            p.paus      = PrefabPaus(ctrlPaus, p);
            p.ouros     = PrefabOuros(ctrlOuros, p);

            Tile tileChao = CriarTile("Chao", "tile_chao");
            Tile tilePlataforma = CriarTile("Plataforma", "tile_plataforma");

            MontarArena(ctrlJogador, p, tileChao, tilePlataforma);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MontarMenu(ctrlJogador, ctrlEspadas, ctrlCopas, ctrlPaus, ctrlOuros, tileChao);

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(CENA_MENU, true),
                new EditorBuildSettingsScene(CENA, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<b>Carta Branca</b>: jogo construido. Abra " + CENA_MENU + " e aperte Play.");
        }

        /// <summary>Referencias que a cena da arena precisa (evita metodos com 12 parametros).</summary>
        class Prefabs
        {
            public GameObject estouro, poeira, anel, brilho, ficha, fichaOuro, carta, espadas, copas, paus, ouros;
        }

        // ------------------------------------------------------------------ linha de comando
        /// <summary>Unity.exe -batchmode -quit -projectPath . -executeMethod CartaBranca.EditorTools.ConstrutorDaArena.ConstruirPeloTerminal</summary>
        public static void ConstruirPeloTerminal()
        {
            Construir();
        }

        /// <summary>Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod CartaBranca.EditorTools.ConstrutorDaArena.WebGLPeloTerminal</summary>
        public static void WebGLPeloTerminal()
        {
            Construir();
            bool ok = GerarWebGL();
            if (!ok) EditorApplication.Exit(1);
        }

        /// <summary>Unity.exe -projectPath . -executeMethod CartaBranca.EditorTools.ConstrutorDaArena.AbrirMenu
        /// (abre o editor ja na cena do menu).</summary>
        [MenuItem("Carta Branca/Abrir cena do menu", false, 10)]
        public static void AbrirMenu()
        {
            EditorSceneManager.OpenScene(CENA_MENU);
        }

        [MenuItem("Carta Branca/Gerar build WebGL", false, 20)]
        public static void GerarWebGLMenu()
        {
            GerarWebGL();
        }

        static bool GerarWebGL()
        {
            PlayerSettings.productName = "Carta Branca - Ultima Mao";
            PlayerSettings.companyName = "Pedro Rossiter";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;   // funciona em qualquer servidor (Unity Play, GitHub Pages)

            BuildPlayerOptions op = new BuildPlayerOptions();
            op.scenes = new[] { CENA_MENU, CENA };
            op.locationPathName = PASTA_WEBGL;
            op.target = BuildTarget.WebGL;
            op.options = BuildOptions.None;

            BuildReport r = BuildPipeline.BuildPlayer(op);
            Debug.Log("<b>Carta Branca</b>: build WebGL " + r.summary.result + " em " + PASTA_WEBGL +
                      " (" + (r.summary.totalSize / (1024 * 1024)) + " MB, " + r.summary.totalErrors + " erros)");
            return r.summary.result == BuildResult.Succeeded;
        }

        // ------------------------------------------------------------------ util
        static void Pasta(string caminho)
        {
            if (!AssetDatabase.IsValidFolder(caminho))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(caminho).Replace('\\', '/'), Path.GetFileName(caminho));
        }

        static Sprite S(string nome)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ART + "/" + nome + ".png");
            if (s == null) Debug.LogWarning("sprite ausente: " + nome);
            return s;
        }

        static void Campo(Object alvo, string propriedade, System.Action<SerializedProperty> ajuste)
        {
            SerializedObject so = new SerializedObject(alvo);
            SerializedProperty p = so.FindProperty(propriedade);
            if (p == null) { Debug.LogWarning("propriedade ausente: " + propriedade); return; }
            ajuste(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ImportarTexturas()
        {
            string abs = Path.Combine(Application.dataPath, "Art");
            if (!Directory.Exists(abs)) { Debug.LogError("pasta Art nao encontrada"); return; }

            foreach (string f in Directory.GetFiles(abs, "*.png", SearchOption.TopDirectoryOnly))
            {
                string caminho = ART + "/" + Path.GetFileName(f);
                TextureImporter ti = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (ti == null) continue;

                string nome = Path.GetFileNameWithoutExtension(caminho);
                bool repete = System.Array.IndexOf(REPETIR, nome) >= 0;
                bool interface_ = nome.StartsWith("ui_");

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = PPU;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaIsTransparency = true;
                ti.wrapMode = repete ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                ti.spriteBorder = interface_ ? new Vector4(4f, 4f, 4f, 4f) : Vector4.zero;   // fatiado em 9

                TextureImporterSettings set = new TextureImporterSettings();
                ti.ReadTextureSettings(set);
                set.spriteMeshType = SpriteMeshType.FullRect;
                set.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(set);
                ti.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ animacao
        static AnimationClip Clipe(string nome, string prefixo, int quadros, float fps, bool loop)
        {
            AnimationClip clip = new AnimationClip();
            clip.frameRate = fps;

            EditorCurveBinding b = new EditorCurveBinding();
            b.type = typeof(SpriteRenderer);
            b.path = "";
            b.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] chaves = new ObjectReferenceKeyframe[quadros];
            for (int i = 0; i < quadros; i++)
            {
                chaves[i] = new ObjectReferenceKeyframe();
                chaves[i].time = i / fps;
                chaves[i].value = S(prefixo + i);
            }
            AnimationUtility.SetObjectReferenceCurve(clip, b, chaves);

            AnimationClipSettings st = AnimationUtility.GetAnimationClipSettings(clip);
            st.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, st);

            string p = ANIM + "/" + nome + ".anim";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(clip, p);
            return clip;
        }

        static AnimatorController ControladorSimples(string nome, string prefixo, int quadros, float fps)
        {
            AnimationClip c = Clipe(nome + "_Loop", prefixo, quadros, fps, true);
            string p = ANIM + "/" + nome + ".controller";
            AssetDatabase.DeleteAsset(p);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(p);
            AnimatorState st = ctrl.layers[0].stateMachine.AddState("Loop");
            st.motion = c;
            ctrl.layers[0].stateMachine.defaultState = st;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static AnimatorController ControladorJogador()
        {
            AnimationClip parado = Clipe("CB_Parado", "cb_parado_", 4, 8f, true);
            AnimationClip corre  = Clipe("CB_Corre",  "cb_corre_",  6, 14f, true);
            AnimationClip pula   = Clipe("CB_Pula",   "cb_pula_",   1, 8f, false);
            AnimationClip cai    = Clipe("CB_Cai",    "cb_cai_",    1, 8f, false);
            AnimationClip saca   = Clipe("CB_Saca",   "cb_saca_",   3, 18f, false);

            string p = ANIM + "/CartaBranca.controller";
            AssetDatabase.DeleteAsset(p);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(p);
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelY", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("NoChao", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Atirar", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState sParado = sm.AddState("Parado", new Vector3(260f, 0f, 0f));   sParado.motion = parado;
            AnimatorState sCorre  = sm.AddState("Corre",  new Vector3(260f, 80f, 0f));  sCorre.motion  = corre;
            AnimatorState sPula   = sm.AddState("Pula",   new Vector3(500f, 0f, 0f));   sPula.motion   = pula;
            AnimatorState sCai    = sm.AddState("Cai",    new Vector3(500f, 80f, 0f));  sCai.motion    = cai;
            AnimatorState sSaca   = sm.AddState("Saca",   new Vector3(260f, 170f, 0f)); sSaca.motion   = saca;
            sm.defaultState = sParado;

            Liga(sParado, sCorre, "Velocidade", AnimatorConditionMode.Greater, 0.15f);
            Liga(sCorre, sParado, "Velocidade", AnimatorConditionMode.Less, 0.15f);

            AnimatorStateTransition qualquerPula = sm.AddAnyStateTransition(sPula);
            qualquerPula.hasExitTime = false; qualquerPula.duration = 0f;
            qualquerPula.canTransitionToSelf = false;
            qualquerPula.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
            qualquerPula.AddCondition(AnimatorConditionMode.Greater, 0.5f, "VelY");

            AnimatorStateTransition qualquerCai = sm.AddAnyStateTransition(sCai);
            qualquerCai.hasExitTime = false; qualquerCai.duration = 0f;
            qualquerCai.canTransitionToSelf = false;
            qualquerCai.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
            qualquerCai.AddCondition(AnimatorConditionMode.Less, 0.5f, "VelY");

            AnimatorStateTransition qualquerSaca = sm.AddAnyStateTransition(sSaca);
            qualquerSaca.hasExitTime = false; qualquerSaca.duration = 0f;
            qualquerSaca.canTransitionToSelf = false;
            qualquerSaca.AddCondition(AnimatorConditionMode.If, 0f, "Atirar");

            Volta(sPula, sParado, "NoChao", true);
            Volta(sCai, sParado, "NoChao", true);
            Volta(sSaca, sParado, null, false);

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void Liga(AnimatorState de, AnimatorState para, string parametro, AnimatorConditionMode modo, float valor)
        {
            AnimatorStateTransition t = de.AddTransition(para);
            t.hasExitTime = false;
            t.duration = 0.02f;
            t.AddCondition(modo, valor, parametro);
        }

        static void Volta(AnimatorState de, AnimatorState para, string parametro, bool exigeVerdadeiro)
        {
            AnimatorStateTransition t = de.AddTransition(para);
            if (parametro == null)
            {
                t.hasExitTime = true;
                t.exitTime = 0.9f;
                t.duration = 0.05f;
            }
            else
            {
                t.hasExitTime = false;
                t.duration = 0.05f;
                t.AddCondition(exigeVerdadeiro ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro);
            }
        }

        // ------------------------------------------------------------------ tiles
        static Tile CriarTile(string nome, string sprite)
        {
            string p = TILES + "/" + nome + ".asset";
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(p);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(t, p);
            }
            t.sprite = S(sprite);
            t.color = Color.white;
            t.colliderType = Tile.ColliderType.Grid;   // colisor = formato da celula
            EditorUtility.SetDirty(t);
            return t;
        }

        /// <summary>Grid + Tilemap + colisor composto (os tiles viram um unico poligono liso,
        /// sem quinas internas para o personagem enganchar).</summary>
        static Tilemap CriarTilemap(string nome, Transform pai, Vector3 celula, Vector3 posicao, int ordem)
        {
            GameObject gradeGO = new GameObject(nome);
            gradeGO.transform.SetParent(pai);
            gradeGO.transform.position = posicao;
            Grid grade = gradeGO.AddComponent<Grid>();
            grade.cellSize = celula;

            GameObject tmGO = new GameObject("Tilemap");
            tmGO.transform.SetParent(gradeGO.transform, false);
            Tilemap tm = tmGO.AddComponent<Tilemap>();
            TilemapRenderer tr = tmGO.AddComponent<TilemapRenderer>();
            tr.sortingOrder = ordem;

            Rigidbody2D rb = tmGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tc = tmGO.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D cc = tmGO.AddComponent<CompositeCollider2D>();
            cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
            return tm;
        }

        /// <summary>O TilemapCollider2D atualiza os formatos de forma preguicosa. Como a cena e salva
        /// logo depois de pintar, e preciso forcar a geracao agora - senao o colisor vai vazio
        /// para a cena e a Carta Branca atravessa o chao.</summary>
        static void FinalizarColisao(Tilemap tm)
        {
            tm.CompressBounds();
            tm.RefreshAllTiles();
            TilemapCollider2D tc = tm.GetComponent<TilemapCollider2D>();
            if (tc != null) tc.ProcessTilemapChanges();
            CompositeCollider2D cc = tm.GetComponent<CompositeCollider2D>();
            if (cc != null)
            {
                cc.GenerateGeometry();
                if (cc.pathCount == 0) Debug.LogError("Tilemap " + tm.transform.parent.name + " ficou sem colisor");
            }
        }

        // ------------------------------------------------------------------ prefabs
        static GameObject Salvar(GameObject go, string nome)
        {
            string p = PREF + "/" + nome + ".prefab";
            GameObject pre = PrefabUtility.SaveAsPrefabAsset(go, p);
            Object.DestroyImmediate(go);
            return pre;
        }

        static GameObject PrefabEfeito(string nome, string sprite, float dur, float cresc, float sobe, float escala, Color cor)
        {
            GameObject go = new GameObject(nome);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = 60;
            sr.color = cor;
            go.transform.localScale = Vector3.one * escala;

            Efeito e = go.AddComponent<Efeito>();
            e.duracao = dur; e.crescimento = cresc; e.subida = sobe;
            return Salvar(go, nome);
        }

        static GameObject PrefabFicha(string nome, string prefixo, int valor, GameObject brilho)
        {
            GameObject go = new GameObject(nome);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(prefixo + "0");
            sr.sortingOrder = 35;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.42f;

            Ficha f = go.AddComponent<Ficha>();
            f.valor = valor;
            f.brilhoPrefab = brilho;
            f.quadros = new Sprite[4];
            for (int i = 0; i < 4; i++) f.quadros[i] = S(prefixo + i);
            return Salvar(go, nome);
        }

        static GameObject PrefabCarta(AnimatorController ctrl)
        {
            GameObject go = new GameObject("Carta");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S("carta_0");
            sr.sortingOrder = 45;

            Animator an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.applyRootMotion = false;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.24f;

            go.AddComponent<Carta>();
            return Salvar(go, "Carta");
        }

        static void CorpoInimigo(GameObject go, string sprite, AnimatorController ctrl, float raio, Vector2 offset)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = 30;

            Animator an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.applyRootMotion = false;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = raio;
            cc.offset = offset;
        }

        static void Identidade(Inimigo i, Prefabs p, Naipe naipe, int pontos, float velocidade, int vida, int fichas)
        {
            i.explosaoPrefab = p.estouro;
            i.poeiraPrefab = p.poeira;
            i.fichaPrefab = p.ficha;
            i.fichaOuroPrefab = p.fichaOuro;
            Campo(i, "naipe", x => x.enumValueIndex = (int)naipe);
            Campo(i, "pontos", x => x.intValue = pontos);
            Campo(i, "velocidadeBase", x => x.floatValue = velocidade);
            Campo(i, "vidaMaxima", x => x.intValue = vida);
            Campo(i, "fichas", x => x.intValue = fichas);
        }

        static GameObject PrefabEspadas(AnimatorController ctrl, Prefabs p)
        {
            GameObject go = new GameObject("NaipeEspadas");
            CorpoInimigo(go, "esp_anda_0", ctrl, 0.5f, new Vector2(0f, -0.1f));
            InimigoEspadas i = go.AddComponent<InimigoEspadas>();
            Identidade(i, p, Naipe.Espadas, 10, 3.1f, 1, 1);
            Campo(i, "alturaPes", x => x.floatValue = 0.72f);
            return Salvar(go, "NaipeEspadas");
        }

        static GameObject PrefabCopas(AnimatorController ctrl, Prefabs p)
        {
            GameObject go = new GameObject("NaipeCopas");
            CorpoInimigo(go, "cop_voa_0", ctrl, 0.46f, Vector2.zero);
            InimigoCopas i = go.AddComponent<InimigoCopas>();
            Identidade(i, p, Naipe.Copas, 18, 4.3f, 1, 2);
            return Salvar(go, "NaipeCopas");
        }

        static GameObject PrefabPaus(AnimatorController ctrl, Prefabs p)
        {
            GameObject go = new GameObject("NaipePaus");
            CorpoInimigo(go, "paus_voa_0", ctrl, 0.48f, Vector2.zero);
            InimigoPaus i = go.AddComponent<InimigoPaus>();
            Identidade(i, p, Naipe.Paus, 25, 2.7f, 2, 3);
            return Salvar(go, "NaipePaus");
        }

        static GameObject PrefabOuros(AnimatorController ctrl, Prefabs p)
        {
            GameObject go = new GameObject("NaipeOuros");
            CorpoInimigo(go, "ouros_voa_0", ctrl, 0.5f, Vector2.zero);
            go.transform.localScale = Vector3.one * 1.15f;
            InimigoOuros i = go.AddComponent<InimigoOuros>();
            Identidade(i, p, Naipe.Ouros, 40, 5.2f, 3, 10);
            return Salvar(go, "NaipeOuros");
        }

        // ------------------------------------------------------------------ cena: pecas comuns
        static GameObject Tiled(string nome, string sprite, Vector3 pos, Vector2 tam, int ordem, Transform pai, Color tinta)
        {
            GameObject go = new GameObject(nome);
            if (pai != null) go.transform.SetParent(pai);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tam;
            sr.sortingOrder = ordem;
            sr.color = tinta;
            return go;
        }

        static Transform Ponto(string nome, Vector3 pos, Transform pai)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(pai);
            go.transform.position = pos;
            return go.transform;
        }

        static void LimparCenaAtual()
        {
            UnityEngine.SceneManagement.Scene cena = EditorSceneManager.GetActiveScene();
            GameObject[] raizes = cena.GetRootGameObjects();
            for (int i = raizes.Length - 1; i >= 0; i--) Object.DestroyImmediate(raizes[i]);
        }

        static Camera CriarCamera(Vector3 posicao)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = INK;
            cam.transform.position = posicao;
            camGO.AddComponent<AudioListener>();

            GameObject ceu = new GameObject("Ceu");
            ceu.transform.SetParent(camGO.transform);
            ceu.transform.localPosition = new Vector3(0f, 0f, 30f);
            ceu.transform.localScale = new Vector3(80f, 5f, 1f);
            SpriteRenderer ceuSR = ceu.AddComponent<SpriteRenderer>();
            ceuSR.sprite = S("ceu");
            ceuSR.sortingOrder = -300;
            return cam;
        }

        static void CriarFundo(bool comParalaxe)
        {
            GameObject fundo = new GameObject("Fundo");

            GameObject lua = new GameObject("Lua");
            lua.transform.SetParent(fundo.transform);
            lua.transform.position = new Vector3(6f, 6.5f, 20f);
            lua.transform.localScale = Vector3.one * 1.6f;
            SpriteRenderer luaSR = lua.AddComponent<SpriteRenderer>();
            luaSR.sprite = S("lua");
            luaSR.sortingOrder = -260;

            GameObject longe = Tiled("HorizonteLonge", "horizonte_longe", new Vector3(0f, -3.4f, 18f),
                                     new Vector2(160f, 3f), -220, fundo.transform, new Color(1f, 1f, 1f, 0.85f));
            GameObject perto = Tiled("HorizontePerto", "horizonte_perto", new Vector3(0f, -4.6f, 16f),
                                     new Vector2(160f, 3f), -210, fundo.transform, Color.white);

            if (!comParalaxe) return;
            Paralaxe pl = lua.AddComponent<Paralaxe>();   pl.fator = 0.08f; pl.alturaFixa = 6.5f;
            Paralaxe p1 = longe.AddComponent<Paralaxe>(); p1.fator = 0.22f; p1.alturaFixa = -3.4f;
            Paralaxe p2 = perto.AddComponent<Paralaxe>(); p2.fator = 0.42f; p2.alturaFixa = -4.6f;
        }

        static void PintarChao(Tilemap chao, Tile tile, int xIni, int xFim)
        {
            for (int x = xIni; x < xFim; x++)
                for (int y = -12; y < -6; y++)
                    chao.SetTile(new Vector3Int(x, y, 0), tile);
        }

        static Sonoplasta CriarSonoplasta()
        {
            GameObject go = new GameObject("Sonoplasta");
            Sonoplasta s = go.AddComponent<Sonoplasta>();
            string[] nomes = System.Enum.GetNames(typeof(Som));
            s.clipes = new AudioClip[nomes.Length];
            for (int i = 0; i < nomes.Length; i++)
            {
                string caminho = AUDIO + "/" + nomes[i].ToLowerInvariant() + ".wav";
                s.clipes[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
                if (s.clipes[i] == null) Debug.LogWarning("som ausente: " + caminho);
            }
            s.musica = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO + "/musica_mesa.wav");
            return s;
        }

        static void CriarEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ------------------------------------------------------------------ UI
        static Transform CriarCanvas(string nome, int ordem)
        {
            GameObject canvasGO = new GameObject(nome, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordem;
            CanvasScaler cs = canvasGO.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            return canvasGO.transform;
        }

        static Text Texto(Transform pai, string nome, Vector2 ancora, Vector2 offset, Vector2 tam,
                          int corpo, TextAnchor alinha, Color cor)
        {
            GameObject go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancora; rt.anchorMax = ancora; rt.pivot = ancora;
            rt.anchoredPosition = offset; rt.sizeDelta = tam;

            Text t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = corpo;
            t.alignment = alinha;
            t.color = cor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.text = "";

            Outline o = go.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.95f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static Image Barra(Transform pai, string nome, Vector2 ancora, Vector2 pos, Vector2 tam, bool comFundo)
        {
            GameObject fundo = new GameObject(nome + "Fundo", typeof(RectTransform));
            fundo.transform.SetParent(pai, false);
            RectTransform frt = fundo.GetComponent<RectTransform>();
            frt.anchorMin = ancora; frt.anchorMax = ancora; frt.pivot = ancora;
            frt.anchoredPosition = pos; frt.sizeDelta = tam;
            if (comFundo)
            {
                Image fi = fundo.AddComponent<Image>();
                fi.sprite = S("pixel");
                fi.color = new Color(0f, 0f, 0f, 0.55f);
                fi.raycastTarget = false;
            }

            GameObject barra = new GameObject(nome, typeof(RectTransform));
            barra.transform.SetParent(fundo.transform, false);
            RectTransform brt = barra.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(2f, 2f); brt.offsetMax = new Vector2(-2f, -2f);
            Image bi = barra.AddComponent<Image>();
            bi.sprite = S("pixel");
            bi.type = Image.Type.Filled;
            bi.fillMethod = Image.FillMethod.Horizontal;
            bi.fillOrigin = (int)Image.OriginHorizontal.Left;
            bi.fillAmount = 1f;
            bi.raycastTarget = false;
            return bi;
        }

        /// <summary>Tela escurecida inteira + caixa central com moldura carmim. Devolve a caixa.</summary>
        static Transform Painel(Transform pai, string nome, Vector2 tamanho, out GameObject raiz)
        {
            raiz = new GameObject(nome, typeof(RectTransform), typeof(Image));
            raiz.transform.SetParent(pai, false);
            RectTransform rrt = raiz.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            Image veu = raiz.GetComponent<Image>();
            veu.sprite = S("pixel");
            veu.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);

            GameObject caixa = new GameObject("Caixa", typeof(RectTransform), typeof(Image));
            caixa.transform.SetParent(raiz.transform, false);
            RectTransform crt = caixa.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = tamanho;
            Image img = caixa.GetComponent<Image>();
            img.sprite = S("ui_painel");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.5f;
            return caixa.transform;
        }

        static Button Botao(Transform pai, string nome, string rotulo, Vector2 pos, Vector2 tam)
        {
            GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;

            Image img = go.GetComponent<Image>();
            img.sprite = S("ui_botao");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.5f;

            Button b = go.GetComponent<Button>();
            ColorBlock cores = b.colors;
            cores.normalColor = AZUL;
            cores.highlightedColor = CARMIM;
            cores.selectedColor = CARMIM;
            cores.pressedColor = OURO;
            cores.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            cores.fadeDuration = 0.06f;
            b.colors = cores;

            Text t = Texto(go.transform, "Rotulo", new Vector2(0.5f, 0.5f), Vector2.zero, tam, 36, TextAnchor.MiddleCenter, PAPEL);
            t.text = rotulo;
            return b;
        }

        // ------------------------------------------------------------------ cena: ARENA
        static void MontarArena(AnimatorController ctrlJogador, Prefabs p, Tile tileChao, Tile tilePlataforma)
        {
            LimparCenaAtual();

            // ---------- camera ----------
            Camera cam = CriarCamera(new Vector3(0f, -3f, -10f));
            CameraSuave rig = cam.gameObject.AddComponent<CameraSuave>();
            rig.limiteEsquerda = -MEIA_LARGURA + 14.5f;
            rig.limiteDireita  =  MEIA_LARGURA - 14.5f;
            rig.limiteBaixo = -3.4f;
            rig.limiteCima = 5f;

            CriarFundo(true);

            // ---------- arena em Tilemap ----------
            GameObject arena = new GameObject("Arena");

            Tilemap chao = CriarTilemap("GradeChao", arena.transform, new Vector3(1f, 1f, 0f), Vector3.zero, 10);
            PintarChao(chao, tileChao, -26, 26);   // celulas y -12..-7: topo em y = -6

            // celulas de 0,5 u deslocadas 0,25 u: o centro da linha k fica em y = 0,5 + 0,5k
            Tilemap plat = CriarTilemap("GradePlataformas", arena.transform, new Vector3(1f, 0.5f, 0f), new Vector3(0f, 0.25f, 0f), 12);
            for (int i = 0; i < PLATAFORMAS.GetLength(0); i++)
            {
                int ini = (int)PLATAFORMAS[i, 0];
                int fim = (int)PLATAFORMAS[i, 1];
                int linha = Mathf.RoundToInt(PLATAFORMAS[i, 2] * 2f - 1f);
                for (int x = ini; x < fim; x++) plat.SetTile(new Vector3Int(x, linha, 0), tilePlataforma);
            }
            FinalizarColisao(chao);
            FinalizarColisao(plat);

            for (int lado = -1; lado <= 1; lado += 2)
            {
                GameObject parede = new GameObject(lado < 0 ? "ParedeEsquerda" : "ParedeDireita");
                parede.transform.SetParent(arena.transform);
                parede.transform.position = new Vector3(lado * (MEIA_LARGURA + 0.5f), 4f, 0f);
                BoxCollider2D bc = parede.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(1f, 40f);
            }

            arena.AddComponent<MapaDeNavegacao>();

            // ---------- sistemas ----------
            GameObject jogoGO = new GameObject("GerenciadorDeJogo");
            jogoGO.AddComponent<GerenciadorDeJogo>();
            CriarSonoplasta();

            // ---------- Carta Branca ----------
            GameObject jog = new GameObject("CartaBranca");
            jog.tag = "Player";
            jog.transform.position = new Vector3(0f, TOPO_CHAO + 1.2f, 0f);

            SpriteRenderer jsr = jog.AddComponent<SpriteRenderer>();
            jsr.sprite = S("cb_parado_0");
            jsr.sortingOrder = 40;

            Animator jan = jog.AddComponent<Animator>();
            jan.runtimeAnimatorController = ctrlJogador;
            jan.applyRootMotion = false;

            Rigidbody2D jrb = jog.AddComponent<Rigidbody2D>();
            jrb.gravityScale = 3.6f;
            jrb.freezeRotation = true;
            jrb.interpolation = RigidbodyInterpolation2D.Interpolate;
            jrb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D jcc = jog.AddComponent<CapsuleCollider2D>();
            jcc.direction = CapsuleDirection2D.Vertical;
            jcc.size = new Vector2(0.9f, 1.75f);
            jcc.offset = new Vector2(0f, 0.02f);

            GameObject mao = new GameObject("Mao");
            mao.transform.SetParent(jog.transform);
            mao.transform.localPosition = new Vector3(0.7f, 0.35f, 0f);

            ControleCartaBranca ctrl = jog.AddComponent<ControleCartaBranca>();
            ctrl.mao = mao.transform;
            ctrl.cartaPrefab = p.carta;
            ctrl.impactoPrefab = p.estouro;
            ctrl.explosaoPrefab = p.estouro;
            ctrl.poeiraPrefab = p.poeira;
            ctrl.animador = jan;
            Campo(ctrl, "vidaMaxima", x => x.intValue = 1);
            Campo(ctrl, "forcaPulo", x => x.floatValue = 17.5f);
            Campo(ctrl, "gravidade", x => x.floatValue = 3.6f);

            UltimaMao ultima = jog.AddComponent<UltimaMao>();
            ultima.anelPrefab = p.anel;
            ultima.cartaSprite = S("carta_0");

            rig.alvo = jog.transform;

            // ---------- gerador ----------
            GameObject gerGO = new GameObject("GeradorDeInimigos");
            GeradorDeInimigos ger = gerGO.AddComponent<GeradorDeInimigos>();
            ger.espadasPrefab = p.espadas;
            ger.copasPrefab = p.copas;
            ger.pausPrefab = p.paus;
            ger.ourosPrefab = p.ouros;

            Transform pastaChao = Ponto("PontosChao", Vector3.zero, gerGO.transform);
            Transform pastaAr   = Ponto("PontosAr", Vector3.zero, gerGO.transform);

            float[] xChao = { -22f, -16f, -9f, 9f, 16f, 22f };
            foreach (float x in xChao)
                ger.pontosChao.Add(Ponto("Chao" + x, new Vector3(x, TOPO_CHAO + 1.5f, 0f), pastaChao));

            float[] xAr = { -19f, -8f, 8f, 19f };
            foreach (float x in xAr)
                ger.pontosAr.Add(Ponto("Ar" + x, new Vector3(x, 8.5f, 0f), pastaAr));

            // ---------- HUD ----------
            Transform ct = CriarCanvas("HUD", 0);
            Hud hud = ct.gameObject.AddComponent<Hud>();
            hud.onda    = Texto(ct, "Onda",    new Vector2(0f, 1f), new Vector2(48f, -40f),  new Vector2(500f, 60f), 46, TextAnchor.UpperLeft, CARMIM);
            hud.pontos  = Texto(ct, "Pontos",  new Vector2(0f, 1f), new Vector2(48f, -100f), new Vector2(700f, 50f), 34, TextAnchor.UpperLeft, PAPEL);
            hud.fichas  = Texto(ct, "Fichas",  new Vector2(0f, 1f), new Vector2(48f, -148f), new Vector2(800f, 50f), 32, TextAnchor.UpperLeft, OURO);
            hud.tempo   = Texto(ct, "Tempo",   new Vector2(1f, 1f), new Vector2(-48f, -40f), new Vector2(400f, 60f), 46, TextAnchor.UpperRight, PAPEL);
            hud.recorde = Texto(ct, "Recorde", new Vector2(1f, 1f), new Vector2(-48f, -100f), new Vector2(500f, 50f), 30, TextAnchor.UpperRight, OURO);
            hud.sequencia = Texto(ct, "Sequencia", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 60f), 44, TextAnchor.UpperCenter, PAPEL);
            hud.barraSequencia = Barra(ct, "BarraSequencia", new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(320f, 12f), false);
            hud.aviso   = Texto(ct, "Aviso",   new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1600f, 70f), 46, TextAnchor.MiddleCenter, PAPEL);
            hud.alertaOuros = Texto(ct, "AlertaOuros", new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1600f, 60f), 38, TextAnchor.MiddleCenter, OURO);
            hud.especial = Texto(ct, "Especial", new Vector2(0f, 0f), new Vector2(48f, 80f), new Vector2(800f, 44f), 32, TextAnchor.LowerLeft, CARMIM);
            hud.ajuda   = Texto(ct, "Ajuda",   new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1700f, 40f), 24, TextAnchor.LowerCenter, CINZA);
            hud.barraEsquiva = Barra(ct, "Esquiva", new Vector2(0f, 0f), new Vector2(48f, 48f), new Vector2(260f, 18f), true);

            // ---------- telas: pausa e fim de jogo ----------
            Transform telasCanvas = CriarCanvas("Telas", 10);
            TelasDaPartida telas = telasCanvas.gameObject.AddComponent<TelasDaPartida>();

            GameObject raizPausa;
            Transform cp = Painel(telasCanvas, "PainelPausa", new Vector2(760f, 600f), out raizPausa);
            Texto(cp, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(700f, 100f), 84, TextAnchor.MiddleCenter, CARMIM).text = "PAUSA";
            Texto(cp, "Frase", new Vector2(0.5f, 0.5f), new Vector2(0f, 115f), new Vector2(700f, 50f), 30, TextAnchor.MiddleCenter, CINZA).text = "a casa espera. as fichas também.";
            telas.continuar        = Botao(cp, "Continuar", "CONTINUAR  [ESC]", new Vector2(0f, 30f), new Vector2(460f, 84f));
            telas.reiniciarNaPausa = Botao(cp, "Reiniciar", "REINICIAR  [R]", new Vector2(0f, -75f), new Vector2(460f, 84f));
            telas.menuNaPausa      = Botao(cp, "Menu", "MENU  [M]", new Vector2(0f, -180f), new Vector2(460f, 84f));
            telas.painelPausa = raizPausa;

            GameObject raizFim;
            Transform cf = Painel(telasCanvas, "PainelFim", new Vector2(1180f, 800f), out raizFim);
            telas.tituloFim = Texto(cf, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(1100f, 110f), 88, TextAnchor.MiddleCenter, CARMIM);
            telas.resumoFim = Texto(cf, "Resumo", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1100f, 440f), 34, TextAnchor.MiddleCenter, PAPEL);
            telas.jogarDeNovo = Botao(cf, "JogarDeNovo", "JOGAR DE NOVO  [R]", new Vector2(-250f, -300f), new Vector2(460f, 90f));
            telas.menuNoFim   = Botao(cf, "Menu", "MENU  [M]", new Vector2(250f, -300f), new Vector2(460f, 90f));
            telas.painelFim = raizFim;

            CriarEventSystem();

            SalvarCena(CENA);
        }

        // ------------------------------------------------------------------ cena: MENU
        static void MontarMenu(AnimatorController ctrlJogador, AnimatorController ctrlEspadas, AnimatorController ctrlCopas,
                               AnimatorController ctrlPaus, AnimatorController ctrlOuros, Tile tileChao)
        {
            LimparCenaAtual();
            CriarCamera(new Vector3(0f, -1f, -10f));
            CriarFundo(false);
            GameObject.Find("Fundo/Lua").transform.position = new Vector3(0f, 5.6f, 20f);   // a lua atras do titulo

            GameObject palco = new GameObject("Palco");
            Tilemap chao = CriarTilemap("GradeChao", palco.transform, new Vector3(1f, 1f, 0f), Vector3.zero, 10);
            PintarChao(chao, tileChao, -20, 20);   // no menu o chao e so cenario: nada colide

            // a Carta Branca em pose de espera e os quatro naipes rondando
            Figurante("CartaBranca", "cb_parado_0", ctrlJogador, new Vector3(-10f, TOPO_CHAO + 2.5f, 0f), 2.5f, 40, 0f, 0f, palco.transform);
            Figurante("Espadas", "esp_anda_0", ctrlEspadas, new Vector3(10.5f, TOPO_CHAO + 1.8f, 0f), -2.2f, 30, 0.05f, 2f, palco.transform);
            Figurante("Copas", "cop_voa_0", ctrlCopas, new Vector3(8.5f, -0.8f, 0f), -2.2f, 30, 0.5f, 1.3f, palco.transform);
            Figurante("Paus", "paus_voa_0", ctrlPaus, new Vector3(12.5f, 2.8f, 0f), -2.2f, 30, 0.35f, 0.9f, palco.transform);
            Figurante("Ouros", "ouros_voa_0", ctrlOuros, new Vector3(-11.5f, 4.5f, 0f), 2.2f, 30, 0.45f, 1.6f, palco.transform);

            CriarSonoplasta();

            Transform ct = CriarCanvas("Menu", 0);
            MenuPrincipal menu = ct.gameObject.AddComponent<MenuPrincipal>();

            // painel principal
            GameObject principal = new GameObject("PainelPrincipal", typeof(RectTransform));
            principal.transform.SetParent(ct, false);
            Esticar(principal);
            Transform pp = principal.transform;
            Texto(pp, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1600f, 170f), 150, TextAnchor.MiddleCenter, PAPEL).text = "CARTA BRANCA";
            Texto(pp, "Subtitulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1200f, 80f), 60, TextAnchor.MiddleCenter, CARMIM).text = "— ÚLTIMA MÃO —";
            Texto(pp, "Frase", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 50f), 30, TextAnchor.MiddleCenter, CINZA).text =
                "a casa nunca perde. você só precisa durar mais uma mão.";
            menu.jogar     = Botao(pp, "Jogar", "JOGAR", new Vector2(0f, 20f), new Vector2(460f, 96f));
            menu.comoJogar = Botao(pp, "ComoJogar", "COMO JOGAR", new Vector2(0f, -100f), new Vector2(460f, 96f));
            menu.sair      = Botao(pp, "Sair", "SAIR", new Vector2(0f, -220f), new Vector2(460f, 96f));
            menu.recorde   = Texto(pp, "Recorde", new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(1400f, 50f), 32, TextAnchor.MiddleCenter, OURO);
            Texto(pp, "Rodape", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1600f, 40f), 22, TextAnchor.LowerCenter, CINZA).text =
                "Desafio Individual Unity 3  ·  Jogos Digitais  ·  arte e som gerados por script";
            menu.painelPrincipal = principal;

            // como jogar
            GameObject raizAjuda;
            Transform ca = Painel(ct, "PainelComoJogar", new Vector2(1500f, 960f), out raizAjuda);
            Texto(ca, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 375f), new Vector2(1400f, 90f), 70, TextAnchor.MiddleCenter, CARMIM).text = "COMO JOGAR";
            Texto(ca, "Texto", new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1340f, 640f), 29, TextAnchor.MiddleLeft, PAPEL).text =
                "Você é a <color=#E9E7DC>Carta Branca</color>, encurralada numa mesa onde a casa nunca perde.\n" +
                "<b>Um toque de qualquer naipe e a sua mão acaba.</b>\n\n" +
                "<color=#C6A258>FICHAS</color>  Naipe derrubado solta fichas. Elas somem em 8 s: vá buscar.\n" +
                "No fim, cada ficha no bolso vale 10 pontos.\n" +
                "<color=#DE465C>SEQUÊNCIA</color>  Abates seguidos multiplicam os pontos (até x5). Pare de acertar e ela quebra.\n" +
                "<color=#DE465C>ÚLTIMA MÃO [L]</color>  Gasta fichas e derruba todos à sua volta. O preço sobe a cada uso,\n" +
                "e quem cai por ela não solta fichas. Guardar ou apostar?\n" +
                "<color=#C6A258>OUROS</color>  Entra a cada onda com o pote. Derrube antes que fuja,\n" +
                "ou a casa cobra 25% do seu bolso e a próxima onda chega na hora.\n" +
                "<color=#96A6C4>PAUS</color>  Calcula o caminho e contorna as plataformas. Não há esconderijo.\n\n" +
                "<color=#96A6C4>A/D correr   ESPAÇO pular   J carta   K esquiva   L última mão   ESC pausa   F2 rotas da IA</color>";
            menu.voltar = Botao(ca, "Voltar", "VOLTAR  [ESC]", new Vector2(0f, -390f), new Vector2(460f, 90f));
            menu.painelComoJogar = raizAjuda;

            CriarEventSystem();
            SalvarCena(CENA_MENU);
        }

        static void Esticar(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void Figurante(string nome, string sprite, AnimatorController ctrl, Vector3 pos, float escala, int ordem,
                              float amplitude, float frequencia, Transform pai)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(pai);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(escala, Mathf.Abs(escala), 1f);   // escala negativa vira o sprite
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = ordem;
            Animator an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.applyRootMotion = false;
            if (amplitude > 0f)
            {
                Flutuar f = go.AddComponent<Flutuar>();
                f.amplitude = amplitude;
                f.frequencia = frequencia;
            }
        }

        static void SalvarCena(string caminho)
        {
            UnityEngine.SceneManagement.Scene cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, caminho);
        }
    }

    /// <summary>Na primeira vez que o projeto abre (ou se a cena do menu ainda nao existe),
    /// monta o jogo sozinho.</summary>
    [InitializeOnLoad]
    public static class ConstrucaoAutomatica
    {
        static ConstrucaoAutomatica()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += Verificar;
        }

        static void Verificar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Verificar;
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ConstrutorDaArena.CENA) != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ConstrutorDaArena.CENA_MENU) != null) return;

            ConstrutorDaArena.Construir();
            EditorSceneManager.OpenScene(ConstrutorDaArena.CENA_MENU);
        }
    }
}
