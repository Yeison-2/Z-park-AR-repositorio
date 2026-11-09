using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

// Crea el HUD por código (horizontal). Todo se dibuja por código: no necesitas imágenes.
public class HUD : MonoBehaviour
{
    static readonly Color Verde = new Color(0.65f, 0.89f, 0.16f);
    static readonly Color Rojo = new Color(0.94f, 0.29f, 0.23f);
    static readonly Color Ambar = new Color(0.88f, 0.66f, 0.24f);
    static readonly Color Fondo = new Color(0f, 0f, 0f, 0.6f);               // oscuro al 60% para exteriores
    static readonly Color FondoPanel = new Color(0.02f, 0.05f, 0.07f, 0.93f);

    static readonly Color Azul = new Color(0.25f, 0.8f, 1f);
    static readonly Color AzulEsfera = new Color(0.12f, 0.45f, 0.9f);
    static readonly Color AzulIcono = new Color(0.8f, 0.97f, 1f);
    static readonly Color RojoBrillo = new Color(1f, 0.35f, 0.28f);
    static readonly Color RojoEsfera = new Color(0.8f, 0.14f, 0.1f);
    static readonly Color RojoIcono = new Color(1f, 0.88f, 0.82f);

    class BotonEsfera
    {
        public RectTransform cuerpo;
        public Image glow;
        public Image segBase;
        public Image seg;
        public Image esfera;
        public Image brillo;
        public Image icono;
        public Text etiqueta;
        public Vector2 iconoPos;
    }

    Text textoPV;
    Text textoPuntos;
    Text textoTiempo;
    Text textoOleada;
    Text textoAviso;
    Text mira;

    GameObject botonesRaiz;
    BotonEsfera atacar;
    BotonEsfera defender;
    BotonMantener botonAtacar;
    Image tinteEscudo;
    DisparoJugador disparo;

    // Pantalla de fin de partida
    GameObject fin;
    RectTransform finPanel;
    Image finOscuro;
    Image finBorde;
    Image finBordePuntos;
    Text finTitulo;
    Text finSub;
    Text finPuntos;
    Text finRanking;
    GameObject finBotones;

    // Menú principal y ranking
    GameObject menu;
    GameObject pantallaRanking;
    bool rankingAbierto;
    InputField campoAlias;
    Text menuMejor;
    readonly List<GameObject> hudJuego = new List<GameObject>();

    // Ranking: tarjeta del mejor y filas de la tabla
    Text rkMejorPuntos, rkMejorAlias, rkMejorDetalle, rkVacio;
    const int FilasRanking = 7;
    readonly Text[,] rkCeldas = new Text[FilasRanking, 7];
    readonly GameObject[] rkFilas = new GameObject[FilasRanking];
    Sprite spCorona;
    Sprite spMira, spPlay, spTrofeo, spCarrito, spEngranaje, spCelular, spCorazon;
    Sprite spFlecha, spPausa, spBlanco;

    // Barra de vida
    Image barraVida;
    RectTransform corazonRT;
    float latidoGolpe;

    // Flechas de zombies fuera de pantalla
    const int MaxFlechas = 6;
    readonly Image[] flechas = new Image[MaxFlechas];
    RectTransform canvasRT;

    // Pausa
    GameObject pantallaPausa;

    // Cómo jugar
    GameObject tutorial;
    Text tutorialBoton;
    bool tutorialDesdeJugar;
    Image jugarBorde;
    Text tiendaSub;

    // Ajustes
    GameObject ajustes;
    GameObject noCompatible;
    Text textoSonido, textoVibra;
    bool sonidoOn = true, vibraOn = true;
    readonly Text[] finValor = new Text[3];
    readonly Text[] finExtra = new Text[3];
    bool finMostrado;
    float finInicio;

    Sprite spCirculo, spEsfera, spBrillo, spGlow, spSegmentos, spAnillo, spBala, spEscudo, spPanel, spBorde;
    Sprite spEstallido, spBalas;

    int ultimaOleada = 0;
    float avisoHasta;
    bool escudoAntes;
    float popDefender;

    // Tienda de mejoras
    class TarjetaMejora
    {
        public RectTransform rt;
        public Image borde;
        public Image botonFondo;
        public Image botonBorde;
        public Text stat;
        public Text costo;
        public Text accion;
        public Image[] segs;
        public Color color;
        public float flash;
    }

    GameObject tienda;
    Text tiendaPuntos;
    RectTransform tiendaPuntosRT;
    Vector2 tiendaPuntosPos;
    readonly TarjetaMejora[] tarjetas = new TarjetaMejora[3];
    Image botonTiendaBorde;
    float errorT;

    // Mensaje mientras se coloca la arena
    GameObject panelEscaneo;
    Image bordeEscaneo;
    Text textoEscaneo;

    // Sonidos generados por código
    AudioSource audioFx;
    AudioClip sCompra, sError, sAbrir;
    AudioClip sDisparo, sMuerte, sGolpe, sBloqueo;

    // Feedback de combate
    Canvas canvasHUD;
    Image vineta;               // borde de color al recibir daño
    Color vinetaColor;
    float danioT;
    float miraKick;             // la mira "salta" al disparar
    float miraAcierto;          // la mira se pone roja al acertar
    Sprite spVineta;

    class Flotante { public Text texto; public Vector3 mundo; public float inicio; }
    readonly List<Flotante> flotantes = new List<Flotante>();

    void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void Start()
    {
        disparo = FindAnyObjectByType<DisparoJugador>();
        CrearEventSystem();
        CrearSprites();

        GameObject cgo = new GameObject("HUD Canvas");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cgo.AddComponent<GraphicRaycaster>();
        CanvasScaler escala = cgo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        canvasHUD = canvas;

        // Viñeta de daño (bordes de la pantalla), detrás de todo el HUD
        GameObject vgo = new GameObject("Vineta Danio", typeof(RectTransform));
        vgo.transform.SetParent(cgo.transform, false);
        vineta = vgo.AddComponent<Image>();
        vineta.sprite = spVineta;
        vineta.raycastTarget = false;
        vineta.enabled = false;
        Estirar(vineta.rectTransform);

        // Tinte azul de pantalla completa mientras el escudo está activo
        GameObject tinte = new GameObject("Tinte Escudo", typeof(RectTransform));
        tinte.transform.SetParent(cgo.transform, false);
        tinteEscudo = tinte.AddComponent<Image>();
        tinteEscudo.color = new Color(Azul.r, Azul.g, Azul.b, 0.16f);
        tinteEscudo.raycastTarget = false;
        Estirar(tinteEscudo.rectTransform);
        tinte.SetActive(false);

        // Barra superior: PV | tiempo | puntos
        textoPV = CrearPanelConTexto("PV", cgo.transform, new Vector2(0, 1), new Vector2(30, -30),
                                     new Vector2(380, 90), 42, Verde);
        CrearBarraVida(textoPV);
        textoTiempo = CrearPanelConTexto("Tiempo", cgo.transform, new Vector2(0.5f, 1), new Vector2(0, -30),
                                         new Vector2(260, 90), 54, Color.white);
        textoPuntos = CrearPanelConTexto("Puntos", cgo.transform, new Vector2(1, 1), new Vector2(-30, -30),
                                         new Vector2(380, 90), 42, Color.white);
        textoOleada = CrearPanelConTexto("Oleada", cgo.transform, new Vector2(0.5f, 1), new Vector2(0, -130),
                                         new Vector2(260, 60), 32, Ambar);

        // Botones en las zonas del pulgar
        botonesRaiz = new GameObject("Botones", typeof(RectTransform));
        botonesRaiz.transform.SetParent(cgo.transform, false);
        Estirar((RectTransform)botonesRaiz.transform);

        float d = 250f;

        defender = CrearBotonEsfera("Defender", botonesRaiz.transform, new Vector2(0, 0), new Vector2(90, 80),
                                    d, spEscudo, "DEFENDER", Azul, AzulEsfera, AzulIcono);
        Button b = defender.cuerpo.gameObject.AddComponent<Button>();
        b.targetGraphic = defender.esfera;
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() =>
        {
            if (GameManager.Instancia != null) GameManager.Instancia.Defender();
        });

        atacar = CrearBotonEsfera("Atacar", botonesRaiz.transform, new Vector2(1, 0), new Vector2(-90, 80),
                                  d, spBala, "ATACAR", RojoBrillo, RojoEsfera, RojoIcono);
        botonAtacar = atacar.cuerpo.gameObject.AddComponent<BotonMantener>();

        // Aviso grande "OLEADA N"
        textoAviso = CrearTexto("Aviso", cgo.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 220),
                                new Vector2(1200, 180), TextAnchor.MiddleCenter, 90, Ambar);

        // Mira en el centro
        mira = CrearTexto("Mira", cgo.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                          new Vector2(200, 200), TextAnchor.MiddleCenter, 70, Verde);
        mira.text = "+";

        // Mensaje de escaneo del suelo (abajo al centro)
        RectTransform esc = CrearCaja("Escaneo", cgo.transform, new Vector2(0, 150), new Vector2(1100, 170),
                                      Verde, out bordeEscaneo, new Vector2(0.5f, 0f));
        panelEscaneo = esc.gameObject;
        textoEscaneo = CrearTexto("Texto", esc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 160),
                                  TextAnchor.MiddleCenter, 44, Verde);
        panelEscaneo.SetActive(false);

        // Sonidos y tienda
        CrearSonidos();
        CrearBotonTienda(botonesRaiz.transform);
        CrearBotonPausa(botonesRaiz.transform);
        canvasRT = (RectTransform)cgo.transform;
        CrearFlechas(cgo.transform);
        CrearTienda(cgo.transform);

        // Escucha los eventos de combate
        if (disparo != null) disparo.AlDisparar += AlDisparar;
        if (GameManager.Instancia != null) GameManager.Instancia.AlGolpe += AlGolpe;
        Zombie.AlMorir += AlMorirZombie;

        // Pantalla de fin, menú y ranking (al final para que queden encima de todo)
        CrearPantallaFin(cgo.transform);
        CrearMenu(cgo.transform);
        CrearRanking(cgo.transform);

        // Partes del HUD que se ocultan en el menú
        hudJuego.Add(textoPV.transform.parent.gameObject);
        hudJuego.Add(textoTiempo.transform.parent.gameObject);
        hudJuego.Add(textoPuntos.transform.parent.gameObject);
        hudJuego.Add(textoOleada.transform.parent.gameObject);
    }

    void Update()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null || textoTiempo == null) return;

#if !UNITY_EDITOR
        // Celular sin ARCore: mensaje claro en lugar del juego
        bool sinAR = ARSession.state == ARSessionState.Unsupported;
        if (noCompatible.activeSelf != sinAR)
        {
            noCompatible.SetActive(sinAR);
            if (sinAR) noCompatible.transform.SetAsLastSibling();
        }
        if (sinAR) return;
#endif

        int segundos = Mathf.CeilToInt(gm.TiempoRestante);
        textoTiempo.text = MinSeg(segundos);
        textoPV.text = "PV " + Mathf.CeilToInt(gm.PV) + " / " + Mathf.CeilToInt(gm.pvMax);
        ActualizarBarraVida(gm);
        textoPuntos.text = "PUNTOS " + gm.Puntos;
        textoOleada.text = "OLEADA " + gm.Oleada + "/" + gm.OleadasTotales;

        bool jugando = gm.JuegoActivo;
        bool preparando = gm.Estado == EstadoPartida.Preparando;
        bool enMenu = gm.Estado == EstadoPartida.Menu;
        menu.SetActive(enMenu && !rankingAbierto);
        pantallaRanking.SetActive(enMenu && rankingAbierto);
        foreach (GameObject g in hudJuego) g.SetActive(!enMenu);
        if (enMenu && jugarBorde != null)
            jugarBorde.color = ConAlfa(Verde, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f));
        botonesRaiz.SetActive(jugando);
        mira.enabled = jugando || preparando;
        ActualizarEscaneo(preparando);
        tinteEscudo.gameObject.SetActive(jugando && gm.EscudoActivo);

        if (jugando)
        {
            ActualizarAtacar();
            ActualizarDefender(gm);
        }
        else if (disparo != null)
        {
            disparo.atacando = false;
        }

        if (jugando && gm.Oleada != ultimaOleada)
        {
            ultimaOleada = gm.Oleada;
            avisoHasta = Time.time + 2.5f;
        }
        textoAviso.text = (jugando && Time.time < avisoHasta) ? "OLEADA " + gm.Oleada : "";

        ActualizarTienda(gm);
        ActualizarCombate(gm, jugando);
        ActualizarFlechas(jugando);
        pantallaPausa.SetActive(gm.Pausado && pausaAbierta);
        ActualizarFin(gm, !gm.Terminada);
    }

    // ---------- Feedback de combate ----------

    void OnDestroy()
    {
        // Importante: el evento de Zombie es estático y sobrevive al reiniciar la escena
        Zombie.AlMorir -= AlMorirZombie;
        if (disparo != null) disparo.AlDisparar -= AlDisparar;
        if (GameManager.Instancia != null) GameManager.Instancia.AlGolpe -= AlGolpe;
    }

    void AlDisparar(bool acerto)
    {
        Sonar(sDisparo);
        miraKick = 1f;
        if (acerto) miraAcierto = 1f;
    }

    void AlMorirZombie(Vector3 pos, int puntos, Color c)
    {
        Sonar(sMuerte);
        Text t = CrearTexto("Flotante", canvasHUD.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                            new Vector2(300, 80), TextAnchor.MiddleCenter, 56, Ambar);
        t.text = "+" + puntos;
        flotantes.Add(new Flotante { texto = t, mundo = pos, inicio = Time.unscaledTime });
    }

    void AlGolpe(bool bloqueado)
    {
        if (bloqueado)
        {
            Sonar(sBloqueo);
            vinetaColor = Azul;
            danioT = 0.7f;
        }
        else
        {
            Sonar(sGolpe);
            vinetaColor = Rojo;
            danioT = 1f;
            latidoGolpe = 1f;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (vibraOn) Handheld.Vibrate();
#endif
        }
    }

    void ActualizarCombate(GameManager gm, bool jugando)
    {
        float dt = Time.unscaledDeltaTime;

        // Viñeta: destello al recibir golpe + latido suave si quedan pocos PV
        danioT = Mathf.MoveTowards(danioT, 0f, dt * 1.5f);
        bool pocaVida = jugando && gm.PV <= gm.pvMax * 0.3f;
        float latido = pocaVida ? 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
        float alfa = Mathf.Max(0.8f * danioT, latido);
        vineta.enabled = alfa > 0.01f;
        vineta.color = ConAlfa(danioT > 0.05f ? vinetaColor : Rojo, alfa);

        // Mira: salta al disparar y se pone roja al acertar
        miraKick = Mathf.MoveTowards(miraKick, 0f, dt * 8f);
        miraAcierto = Mathf.MoveTowards(miraAcierto, 0f, dt * 4f);
        mira.rectTransform.localScale = Vector3.one * (1f + 0.5f * miraKick);
        mira.color = Color.Lerp(Verde, Rojo, miraAcierto);

        // "+puntos" que suben y se desvanecen sobre el zombie que murió
        Camera cam = Camera.main;
        float escala = canvasHUD != null ? canvasHUD.scaleFactor : 1f;
        for (int i = flotantes.Count - 1; i >= 0; i--)
        {
            Flotante f = flotantes[i];
            float edad = Time.unscaledTime - f.inicio;
            if (edad > 0.9f || cam == null)
            {
                Destroy(f.texto.gameObject);
                flotantes.RemoveAt(i);
                continue;
            }
            Vector3 sp = cam.WorldToScreenPoint(f.mundo);
            f.texto.enabled = sp.z > 0f;
            f.texto.rectTransform.position = new Vector3(sp.x, sp.y + edad * 150f * escala, 0f);
            f.texto.color = ConAlfa(Ambar, 1f - edad / 0.9f);
            f.texto.rectTransform.localScale = Vector3.one * (1f + 0.3f * Mathf.Max(0f, 1f - edad * 5f));
        }
    }

    void ActualizarEscaneo(bool preparando)
    {
        panelEscaneo.SetActive(preparando);
        if (!preparando) return;

        Arena a = Arena.Instancia;
        bool apunta = a != null && a.ApuntaAlSuelo;
        if (apunta)
        {
            textoEscaneo.text = "TOCA LA PANTALLA PARA COLOCAR LA ARENA\n" +
                                "<size=30>El anillo grande marca el campo de batalla</size>";
        }
        else if (a != null && a.RespaldoDisponible)
        {
            textoEscaneo.text = "NO SE DETECTA EL SUELO\n" +
                                "<size=30>Apunta al piso con buena luz, o toca la pantalla para jugar igual</size>";
        }
        else if (a != null && a.HaySuelo)
        {
            textoEscaneo.text = "APUNTA LA MIRA AL SUELO\n" +
                                "<size=30>Busca una zona plana y despejada</size>";
        }
        else
        {
            textoEscaneo.text = "ESCANEA EL SUELO\n" +
                                "<size=30>Mueve el celular lentamente apuntando al piso</size>";
        }

        // Si el AR del celular dio un error, se muestra para poder reportarlo con una captura
        if (a != null && !string.IsNullOrEmpty(a.UltimoError))
        {
            string err = a.UltimoError.Length > 110 ? a.UltimoError.Substring(0, 110) + "…" : a.UltimoError;
            textoEscaneo.text += "\n<size=20><color=#F04A3A>Error AR: " + err + "</color></size>";
        }

        float a2 = apunta ? 1f : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        bordeEscaneo.color = ConAlfa(Verde, a2);
    }

    // ---------- Tienda de mejoras ----------

    static readonly string[] MejoraTitulo = { "POTENCIA DE DISPARO", "CADENCIA / PROYECTILES", "ESCUDO / ENFRIAMIENTO" };
    static readonly string[] MejoraDesc =
    {
        "Aumenta el daño\nde cada disparo",
        "Más disparos por segundo\nNiv. 3 y 6: +1 bala",
        "Recarga más rápida\ny escudo más largo"
    };

    void CrearBotonTienda(Transform padre)
    {
        // Encima de ATACAR
        RectTransform rt = CrearCaja("Boton Tienda", padre, new Vector2(-215, 415), new Vector2(210, 120),
                                     Verde, out botonTiendaBorde, new Vector2(1, 0));
        Image fondo = rt.GetComponent<Image>();
        fondo.raycastTarget = true;
        fondo.color = new Color(0f, 0f, 0f, 0.7f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(AbrirTienda);

        CrearTexto("Texto", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 110),
                   TextAnchor.MiddleCenter, 34, Verde).text = "TIENDA\n<size=22>DE MEJORAS</size>";
    }

    void CrearTienda(Transform canvas)
    {
        tienda = new GameObject("Tienda", typeof(RectTransform));
        tienda.transform.SetParent(canvas, false);
        Estirar((RectTransform)tienda.transform);

        // Fondo oscuro que además bloquea los toques al juego
        Image oscuro = tienda.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.6f);
        oscuro.raycastTarget = true;

        RectTransform panel = CrearCaja("Panel", tienda.transform, Vector2.zero, new Vector2(1560, 800), Verde, out _);

        CrearTexto("Titulo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 335), new Vector2(1200, 80),
                   TextAnchor.MiddleCenter, 58, Verde).text = "TIENDA DE MEJORAS";
        tiendaSub = CrearTexto("Subtitulo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 285), new Vector2(1200, 40),
                               TextAnchor.MiddleCenter, 26, new Color(0.8f, 0.8f, 0.8f));
        tiendaSub.text = "INVIERTE TUS PUNTOS Y FORTALECE TU EQUIPO";

        // Botón cerrar (X)
        RectTransform cerrar = CrearCaja("Cerrar", panel, new Vector2(700, 335), new Vector2(100, 100), Color.white, out _);
        cerrar.GetComponent<Image>().raycastTarget = true;
        Button bc = cerrar.gameObject.AddComponent<Button>();
        bc.transition = Selectable.Transition.None;
        bc.onClick.AddListener(CerrarTienda);
        CrearTexto("X", cerrar, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100),
                   TextAnchor.MiddleCenter, 52, Color.white).text = "X";

        // Puntos actuales
        RectTransform cajaPuntos = CrearCaja("Puntos", panel, new Vector2(0, 205), new Vector2(760, 85), Ambar, out _);
        tiendaPuntos = CrearTexto("Texto", cajaPuntos, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740, 80),
                                  TextAnchor.MiddleCenter, 40, Color.white);
        tiendaPuntosRT = tiendaPuntos.rectTransform;
        tiendaPuntosPos = tiendaPuntosRT.anchoredPosition;

        // Tres tarjetas lado a lado
        Color[] colores = { RojoBrillo, Azul, Verde };
        Sprite[] iconos = { spEstallido, spBalas, spEscudo };
        for (int i = 0; i < 3; i++)
            tarjetas[i] = CrearTarjeta(i, panel, new Vector2(-510 + i * 510, -95), colores[i], iconos[i]);

        tienda.SetActive(false);
    }

    TarjetaMejora CrearTarjeta(int i, Transform padre, Vector2 pos, Color color, Sprite icono)
    {
        TarjetaMejora t = new TarjetaMejora();
        t.color = color;
        t.rt = CrearCaja(MejoraTitulo[i], padre, pos, new Vector2(470, 490), color, out t.borde);

        // Ícono dentro de una esfera
        Image bola = CrearImagen("Esfera", t.rt, spEsfera, new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f));
        CentrarTam(bola.rectTransform, 120, new Vector2(0, 165));
        Image aro = CrearImagen("Anillo", t.rt, spAnillo, color);
        CentrarTam(aro.rectTransform, 120, new Vector2(0, 165));
        Image ic = CrearImagen("Icono", t.rt, icono, color);
        CentrarTam(ic.rectTransform, 74, new Vector2(0, 165));

        CrearTexto("Titulo", t.rt, new Vector2(0.5f, 0.5f), new Vector2(0, 78), new Vector2(460, 44),
                   TextAnchor.MiddleCenter, 30, color).text = MejoraTitulo[i];
        CrearTexto("Desc", t.rt, new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(460, 70),
                   TextAnchor.MiddleCenter, 23, new Color(0.8f, 0.8f, 0.8f)).text = MejoraDesc[i];
        t.stat = CrearTexto("Stat", t.rt, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(460, 40),
                            TextAnchor.MiddleCenter, 25, Color.white);

        // Barra de niveles
        GameManager gm = GameManager.Instancia;
        int niveles = gm != null ? gm.NivelMax : 6;
        t.segs = new Image[niveles];
        float ancho = 56f, paso = 66f;
        for (int k = 0; k < niveles; k++)
        {
            GameObject sg = new GameObject("Nivel " + (k + 1), typeof(RectTransform));
            sg.transform.SetParent(t.rt, false);
            Image im = sg.AddComponent<Image>();
            im.raycastTarget = false;
            RectTransform r = im.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(ancho, 18);
            r.anchoredPosition = new Vector2((k - (niveles - 1) * 0.5f) * paso, -88);
            t.segs[k] = im;
        }

        // Botón MEJORAR
        RectTransform boton = CrearCaja("Mejorar", t.rt, new Vector2(0, -175), new Vector2(340, 105), color, out t.botonBorde);
        t.botonFondo = boton.GetComponent<Image>();
        t.botonFondo.raycastTarget = true;
        Button b = boton.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        int idx = i;
        b.onClick.AddListener(() => IntentarComprar(idx));

        t.costo = CrearTexto("Costo", boton, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(330, 44),
                             TextAnchor.MiddleCenter, 34, Color.white);
        t.accion = CrearTexto("Accion", boton, new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(330, 36),
                              TextAnchor.MiddleCenter, 26, color);
        return t;
    }

    void AbrirTienda()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null || !gm.JuegoActivo) return;
        gm.Pausar(true);
        tienda.SetActive(true);
        Sonar(sAbrir);
    }

    void CerrarTienda()
    {
        tienda.SetActive(false);
        tiendaSub.text = "INVIERTE TUS PUNTOS Y FORTALECE TU EQUIPO";
        if (GameManager.Instancia != null) GameManager.Instancia.Pausar(false);
        Sonar(sAbrir);
    }

    void IntentarComprar(int i)
    {
        GameManager gm = GameManager.Instancia;
        if (gm != null && gm.Comprar(i))
        {
            tarjetas[i].flash = 1f;
            Sonar(sCompra);
        }
        else
        {
            errorT = 1f;
            Sonar(sError);
        }
    }

    void ActualizarTienda(GameManager gm)
    {
        // El botón TIENDA late cuando alcanza para alguna mejora
        bool alcanza = gm.PuedeComprar(0) || gm.PuedeComprar(1) || gm.PuedeComprar(2);
        botonTiendaBorde.color = alcanza
            ? ConAlfa(Verde, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f))
            : ConAlfa(Verde, 0.35f);

        if (!tienda.activeSelf) return;
        float dt = Time.unscaledDeltaTime;

        // Puntos (tiemblan en rojo si no alcanza)
        errorT = Mathf.MoveTowards(errorT, 0f, dt * 3f);
        tiendaPuntos.text = "TUS PUNTOS ACTUALES:  <color=#A6E32A>" + gm.Puntos + "</color>";
        tiendaPuntos.color = Color.Lerp(Color.white, Rojo, errorT);
        tiendaPuntosRT.anchoredPosition = tiendaPuntosPos +
            new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 12f * errorT, 0f);

        DisparoJugador d = disparo;
        for (int i = 0; i < 3; i++)
        {
            TarjetaMejora t = tarjetas[i];
            int n = gm.NivelMejora[i];
            int costo = gm.CostoMejora(i);
            bool max = costo < 0;
            bool puede = gm.PuedeComprar(i);

            for (int k = 0; k < t.segs.Length; k++)
                t.segs[k].color = k < n ? t.color : ConAlfa(t.color, 0.18f);

            t.costo.text = max ? "NIVEL MÁXIMO" : costo + " PTS";
            t.accion.text = max ? "" : "MEJORAR";
            t.costo.color = (puede || max) ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            t.botonFondo.color = puede
                ? new Color(t.color.r * 0.35f, t.color.g * 0.35f, t.color.b * 0.35f, 0.95f)
                : FondoPanel;
            t.botonBorde.color = puede ? t.color : ConAlfa(t.color, 0.3f);

            // Destello al comprar
            t.flash = Mathf.MoveTowards(t.flash, 0f, dt * 2.5f);
            t.rt.localScale = Vector3.one * (1f + 0.06f * t.flash);
            t.borde.color = Color.Lerp(t.color, Color.white, t.flash);

            // Valor actual → siguiente nivel
            string flecha = max ? "" : "  →  ";
            if (i == 0 && d != null)
                t.stat.text = "Daño: " + d.danio.ToString("0.0") + flecha + (max ? "" : (d.danio + gm.danioPorNivel).ToString("0.0"));
            else if (i == 1 && d != null)
                t.stat.text = "Disparos/s: " + d.cadencia.ToString("0.0") + flecha +
                              (max ? "" : (d.cadencia + gm.cadenciaPorNivel).ToString("0.0")) +
                              "   Balas: " + d.proyectiles;
            else if (i == 2)
                t.stat.text = "Recarga: " + gm.cooldownEscudo.ToString("0.0") + " s" + flecha +
                              (max ? "" : Mathf.Max(1f, gm.cooldownEscudo - gm.cooldownMenosPorNivel).ToString("0.0") + " s");
        }
    }

    // ---------- Sonidos ----------

    void CrearSonidos()
    {
        audioFx = gameObject.AddComponent<AudioSource>();
        audioFx.playOnAwake = false;
        audioFx.spatialBlend = 0f;

        sCompra = HacerTono("Compra", new float[] { 660f, 880f, 1320f }, 0.09f, 0.5f, false);
        sError = HacerTono("Error", new float[] { 160f, 120f }, 0.12f, 0.3f, true);
        sAbrir = HacerTono("Abrir", new float[] { 520f }, 0.07f, 0.3f, false);
        sBloqueo = HacerTono("Bloqueo", new float[] { 1200f, 1600f }, 0.06f, 0.25f, true);

        // Disparo: ruido seco + golpe grave
        sDisparo = HacerSonido("Disparo", 0.14f, t =>
            Random.Range(-1f, 1f) * Mathf.Exp(-t * 35f) * 0.45f +
            Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 25f) * 0.45f);

        // Muerte: "splat" con tono que baja
        sMuerte = HacerSonido("Muerte", 0.3f, t =>
            Random.Range(-1f, 1f) * Mathf.Exp(-t * 12f) * 0.35f +
            Mathf.Sin(2f * Mathf.PI * (300f - 500f * t) * t) * Mathf.Exp(-t * 8f) * 0.4f);

        // Golpe al jugador: golpe grave
        sGolpe = HacerSonido("Golpe", 0.25f, t =>
            Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 10f) * 0.8f +
            Random.Range(-1f, 1f) * Mathf.Exp(-t * 30f) * 0.3f);
    }

    // Sonido a partir de una fórmula f(t)
    static AudioClip HacerSonido(string nombre, float dur, System.Func<float, float> f)
    {
        const int sr = 44100;
        int n = Mathf.RoundToInt(sr * dur);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(f(i / (float)sr) * Mathf.Clamp01(i / 100f), -1f, 1f);
        AudioClip clip = AudioClip.Create(nombre, n, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Sonar(AudioClip c)
    {
        if (audioFx != null && c != null) audioFx.PlayOneShot(c);
    }

    // Notas cortas una tras otra (seno o cuadrada) con caída suave
    static AudioClip HacerTono(string nombre, float[] frecs, float durNota, float vol, bool cuadrada)
    {
        const int sr = 44100;
        int porNota = Mathf.RoundToInt(sr * durNota);
        float[] data = new float[porNota * frecs.Length];
        for (int n = 0; n < frecs.Length; n++)
        {
            for (int i = 0; i < porNota; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Exp(-t * 8f) * Mathf.Clamp01(i / 200f);
                float s = Mathf.Sin(2f * Mathf.PI * frecs[n] * t);
                if (cuadrada) s = Mathf.Sign(s) * 0.5f;
                data[n * porNota + i] = s * env * vol;
            }
        }
        AudioClip clip = AudioClip.Create(nombre, data.Length, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    static string MinSeg(int s)
    {
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    void ActualizarAtacar()
    {
        bool p = botonAtacar.Presionado;
        if (disparo != null) disparo.atacando = p;

        float objetivo = p ? 0.9f : 1f;
        atacar.cuerpo.localScale = Vector3.Lerp(atacar.cuerpo.localScale, Vector3.one * objetivo,
                                                Time.unscaledDeltaTime * 20f);
        atacar.esfera.color = p ? RojoBrillo : RojoEsfera;
        float g = p ? 0.95f : 0.45f + 0.1f * Mathf.Sin(Time.time * 3f);
        atacar.glow.color = ConAlfa(RojoBrillo, g);
        atacar.icono.rectTransform.anchoredPosition =
            atacar.iconoPos + (p ? Random.insideUnitCircle * 5f : Vector2.zero);
    }

    void ActualizarDefender(GameManager gm)
    {
        float cd = gm.CooldownRestante;
        bool esc = gm.EscudoActivo;

        if (esc && !escudoAntes) popDefender = 1f;
        escudoAntes = esc;
        popDefender = Mathf.MoveTowards(popDefender, 0f, Time.deltaTime * 4f);
        defender.cuerpo.localScale = Vector3.one * (1f + 0.15f * popDefender);

        if (esc)
        {
            defender.seg.fillAmount = 1f;
            defender.esfera.color = Color.Lerp(AzulEsfera, Azul, 0.6f);
            defender.glow.color = ConAlfa(Azul, 0.8f + 0.2f * Mathf.Sin(Time.time * 12f));
            defender.icono.color = Color.white;
            defender.etiqueta.text = "ESCUDO";
        }
        else if (cd > 0f)
        {
            float carga = gm.cooldownEscudo > 0f ? 1f - Mathf.Clamp01(cd / gm.cooldownEscudo) : 1f;
            defender.seg.fillAmount = carga;
            defender.esfera.color = new Color(0.12f, 0.2f, 0.3f);
            defender.glow.color = ConAlfa(Azul, 0.1f);
            defender.icono.color = ConAlfa(AzulIcono, 0.35f);
            defender.etiqueta.text = Mathf.CeilToInt(cd) + " s";
        }
        else
        {
            defender.seg.fillAmount = 1f;
            defender.esfera.color = AzulEsfera;
            defender.glow.color = ConAlfa(Azul, 0.45f + 0.25f * Mathf.Sin(Time.time * 4f));
            defender.icono.color = AzulIcono;
            defender.etiqueta.text = "DEFENDER";
        }
    }

    // ---------- Pantalla de fin ----------

    void CrearPantallaFin(Transform canvas)
    {
        fin = new GameObject("Fin de partida", typeof(RectTransform));
        fin.transform.SetParent(canvas, false);
        Estirar((RectTransform)fin.transform);

        // Oscurece el juego detrás del panel
        finOscuro = fin.AddComponent<Image>();
        finOscuro.color = new Color(0f, 0f, 0f, 0.55f);
        finOscuro.raycastTarget = false;

        // Panel principal
        finPanel = CrearCaja("Panel", fin.transform, Vector2.zero, new Vector2(1150, 740), Rojo, out finBorde);

        finTitulo = CrearTexto("Titulo", finPanel, new Vector2(0.5f, 0.5f), new Vector2(0, 290),
                               new Vector2(1100, 120), TextAnchor.MiddleCenter, 96, Rojo);
        finSub = CrearTexto("Subtitulo", finPanel, new Vector2(0.5f, 0.5f), new Vector2(0, 218),
                            new Vector2(1100, 50), TextAnchor.MiddleCenter, 32, new Color(0.85f, 0.85f, 0.85f));

        // Puntuación final
        finRanking = CrearTexto("Ranking", finPanel, new Vector2(0.5f, 0.5f), new Vector2(0, 172),
                                new Vector2(1100, 40), TextAnchor.MiddleCenter, 30, Ambar);

        RectTransform cajaPuntos = CrearCaja("Puntuacion", finPanel, new Vector2(0, 65), new Vector2(580, 160),
                                             Ambar, out finBordePuntos);
        CrearTexto("Etiqueta", cajaPuntos, new Vector2(0.5f, 0.5f), new Vector2(0, 50),
                   new Vector2(560, 40), TextAnchor.MiddleCenter, 28, Ambar).text = "PUNTUACIÓN FINAL";
        finPuntos = CrearTexto("Valor", cajaPuntos, new Vector2(0.5f, 0.5f), new Vector2(0, -18),
                               new Vector2(560, 100), TextAnchor.MiddleCenter, 92, Color.white);

        // Tres tarjetas: zombies, tiempo, oleadas
        string[] titulos = { "ZOMBIES", "TIEMPO", "OLEADAS" };
        Color[] colores = { RojoBrillo, Azul, Verde };
        for (int i = 0; i < 3; i++)
        {
            RectTransform tarjeta = CrearCaja(titulos[i], finPanel, new Vector2(-350 + i * 350, -125),
                                              new Vector2(320, 170), colores[i], out _);
            CrearTexto("Titulo", tarjeta, new Vector2(0.5f, 0.5f), new Vector2(0, 52),
                       new Vector2(300, 36), TextAnchor.MiddleCenter, 26, colores[i]).text = titulos[i];
            finValor[i] = CrearTexto("Valor", tarjeta, new Vector2(0.5f, 0.5f), new Vector2(0, -2),
                                     new Vector2(300, 70), TextAnchor.MiddleCenter, 58, Color.white);
            finExtra[i] = CrearTexto("Extra", tarjeta, new Vector2(0.5f, 0.5f), new Vector2(0, -55),
                                     new Vector2(300, 36), TextAnchor.MiddleCenter, 26, Ambar);
        }

        // Botones (aparecen tras 1 s para no tocarlos sin querer mientras disparabas)
        finBotones = new GameObject("Botones Fin", typeof(RectTransform));
        finBotones.transform.SetParent(finPanel, false);
        Estirar((RectTransform)finBotones.transform);
        CrearBotonCaja("Jugar de nuevo", finBotones.transform, new Vector2(-190, -295), new Vector2(460, 95),
                       "JUGAR DE NUEVO", 40, Verde, () =>
                       {
                           Sonar(sAbrir);
                           if (GameManager.Instancia != null) GameManager.Instancia.JugarDeNuevo();
                       });
        CrearBotonCaja("Menu", finBotones.transform, new Vector2(240, -295), new Vector2(300, 95),
                       "MENÚ", 40, Ambar, () =>
                       {
                           Sonar(sAbrir);
                           if (GameManager.Instancia != null) GameManager.Instancia.IrAlMenu();
                       });
        finBotones.SetActive(false);

        fin.SetActive(false);
    }

    void ActualizarFin(GameManager gm, bool jugando)
    {
        if (jugando)
        {
            if (finMostrado) { finMostrado = false; fin.SetActive(false); }
            return;
        }

        if (!finMostrado)
        {
            finMostrado = true;
            finInicio = Time.unscaledTime;
            LlenarFin(gm);
            fin.SetActive(true);
        }

        // Aparece con un pequeño zoom y el texto de reinicio parpadea cuando ya se puede tocar
        float t = Mathf.Clamp01((Time.unscaledTime - finInicio) / 0.35f);
        float e = 1f - Mathf.Pow(1f - t, 3f);
        finPanel.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
        finOscuro.color = new Color(0f, 0f, 0f, 0.55f * e);

        bool listo = Time.unscaledTime - finInicio >= 1f;
        if (finBotones.activeSelf != listo) finBotones.SetActive(listo);
    }

    void LlenarFin(GameManager gm)
    {
        bool gano = gm.Estado == EstadoPartida.Victoria;
        Color c = gano ? Verde : Rojo;

        finTitulo.text = gano ? "SOBREVIVIENTE" : "INFECTADO";
        finTitulo.color = c;
        finBorde.color = c;
        finSub.text = gano ? "Resististe las " + gm.OleadasTotales + " oleadas"
                           : "Los zombies te alcanzaron";

        int seg = Mathf.FloorToInt(gm.TiempoTranscurrido);
        int ola = gm.OleadasCompletadas;

        finPuntos.text = gm.PuntuacionFinal.ToString();
        finValor[0].text = gm.ZombiesEliminados.ToString();
        finValor[1].text = MinSeg(seg);
        finValor[2].text = ola + "/" + gm.OleadasTotales;

        finExtra[0].text = "+" + (gm.ZombiesEliminados * gm.puntosPorZombie) + " pts";
        finExtra[1].text = "+" + (seg * gm.puntosPorSegundo) + " pts";
        finExtra[2].text = "+" + (ola * gm.puntosPorOleada) + " pts";

        string alias = string.IsNullOrEmpty(Ranking.Alias) ? "JUGADOR" : Ranking.Alias;
        if (gm.PuestoRanking == 1)
            finRanking.text = "¡NUEVO RÉCORD, " + alias + "!";
        else if (gm.PuestoRanking > 1)
            finRanking.text = alias + "  ·  PUESTO #" + gm.PuestoRanking + " EN EL RANKING";
        else
            finRanking.text = alias;
    }

    // Botón: caja con esquinas cortadas, borde de color y texto
    RectTransform CrearBotonCaja(string nombre, Transform padre, Vector2 pos, Vector2 tam, string texto,
                                 int tamTexto, Color color, System.Action accion)
    {
        RectTransform rt = CrearCaja(nombre, padre, pos, tam, color, out _);
        Image fondo = rt.GetComponent<Image>();
        fondo.raycastTarget = true;
        fondo.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.95f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => accion());
        CrearTexto("Texto", rt, new Vector2(0.5f, 0.5f), Vector2.zero, tam, TextAnchor.MiddleCenter,
                   tamTexto, color).text = texto;
        return rt;
    }

    // ---------- Menú principal ----------

    void CrearMenu(Transform canvas)
    {
        menu = new GameObject("Menu", typeof(RectTransform));
        menu.transform.SetParent(canvas, false);
        Estirar((RectTransform)menu.transform);
        Image oscuro = menu.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.55f);
        oscuro.raycastTarget = true;

        // Etiqueta "AR SURVIVAL" (arriba a la izquierda)
        RectTransform tag = CrearCaja("Etiqueta AR", menu.transform, new Vector2(190, -60), new Vector2(300, 64),
                                      ConAlfa(Azul, 0.7f), out _, new Vector2(0f, 1f));
        Image icTag = CrearImagen("Icono", tag, spMira, Azul);
        CentrarTam(icTag.rectTransform, 40, new Vector2(-105, 0));
        CrearTexto("Texto", tag, new Vector2(0.5f, 0.5f), new Vector2(25, 0), new Vector2(220, 60),
                   TextAnchor.MiddleCenter, 26, Color.white).text = "AR SURVIVAL";

        // Ajustes (engranaje, arriba a la derecha)
        RectTransform eng = CrearCaja("Ajustes", menu.transform, new Vector2(-80, -70), new Vector2(110, 110),
                                      ConAlfa(Color.white, 0.6f), out _, new Vector2(1f, 1f));
        eng.GetComponent<Image>().raycastTarget = true;
        Button be = eng.gameObject.AddComponent<Button>();
        be.transition = Selectable.Transition.None;
        be.onClick.AddListener(() => { Sonar(sAbrir); ajustes.SetActive(true); ajustes.transform.SetAsLastSibling(); });
        Image icEng = CrearImagen("Icono", eng, spEngranaje, Color.white);
        CentrarTam(icEng.rectTransform, 64, Vector2.zero);

        // Récord (junto al engranaje)
        menuMejor = CrearTexto("Mejor", menu.transform, new Vector2(1f, 1f), new Vector2(-160, -48),
                               new Vector2(600, 50), TextAnchor.MiddleRight, 28, Ambar);
        Partida mejor = Ranking.Mejor();
        menuMejor.text = mejor != null ? "RÉCORD: " + mejor.puntaje + "  (" + mejor.alias + ")" : "";

        // Logo: si existe la imagen Assets/Resources/ZPark_Logo.png la usa; si no, lo dibuja con texto
        Sprite spLogo = CargarLogo();
        if (spLogo != null)
        {
            Image logoImg = CrearImagen("Logo", menu.transform, spLogo, Color.white);
            logoImg.preserveAspect = true;
            CentrarTam(logoImg.rectTransform, 0, new Vector2(0, 300));
            logoImg.rectTransform.sizeDelta = new Vector2(1000, 260);
        }
        else
        {
            // Pincelada roja inclinada detrás de la Z
            Image brocha = CrearImagen("Brocha", menu.transform, null, new Color(0.85f, 0.12f, 0.1f, 0.9f));
            CentrarTam(brocha.rectTransform, 0, new Vector2(-335, 300));
            brocha.rectTransform.sizeDelta = new Vector2(250, 190);
            brocha.rectTransform.localRotation = Quaternion.Euler(0, 0, -8f);

            Text logo = CrearTexto("Logo", menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 300),
                                   new Vector2(1200, 230), TextAnchor.MiddleCenter, 210, Color.white);
            logo.fontStyle = FontStyle.BoldAndItalic;
            logo.text = "Z-PARK";
            logo.GetComponent<Outline>().effectDistance = new Vector2(7, -7);
        }

        // Lema con líneas a los lados
        CrearTexto("Lema", menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 165), new Vector2(1000, 44),
                   TextAnchor.MiddleCenter, 30, Color.white).text = "SOBREVIVE  ·  MEJORA  ·  RÉCORD";
        for (int k = -1; k <= 1; k += 2)
        {
            Image linea = CrearImagen("Linea", menu.transform, null, ConAlfa(Rojo, 0.8f));
            CentrarTam(linea.rectTransform, 0, new Vector2(k * 390, 165));
            linea.rectTransform.sizeDelta = new Vector2(150, 4);
        }

        // Alias
        CrearTexto("Etiqueta Alias", menu.transform, new Vector2(0.5f, 0.5f), new Vector2(-400, 75),
                   new Vector2(240, 50), TextAnchor.MiddleRight, 30, Ambar).text = "TU ALIAS";
        campoAlias = CrearCampoTexto(menu.transform, new Vector2(0, 75), new Vector2(520, 80));
        campoAlias.text = Ranking.Alias;

        // JUGAR (grande, verde, con triángulo)
        CrearBotonMenu("Jugar", menu.transform, new Vector2(0, -70), new Vector2(720, 140), spPlay,
                       "JUGAR", "", 78, Verde, AlTocarJugar, out jugarBorde);

        // RANKING y TIENDA (azules, lado a lado)
        CrearBotonMenu("Ranking", menu.transform, new Vector2(-185, -230), new Vector2(350, 115), spTrofeo,
                       "RANKING", "", 36, Azul, () =>
                       {
                           Sonar(sAbrir);
                           RefrescarRanking();
                           rankingAbierto = true;
                       }, out _);
        CrearBotonMenu("Tienda", menu.transform, new Vector2(185, -230), new Vector2(350, 115), spCarrito,
                       "TIENDA", "MEJORAS", 36, Azul, AbrirTiendaMenu, out _);

        // Datos de abajo
        CrearChip(menu.transform, new Vector2(-330, 65), spCelular, "REALIDAD AUMENTADA", "Juega en el parque");
        CrearChip(menu.transform, new Vector2(330, 65), spCorazon, "5 MINUTOS", "¿Cuánto puedes resistir?");

        menu.SetActive(false);

        CrearAjustes(canvas);
        CrearPausa(canvas);
        CrearTutorial(canvas);
        CrearNoCompatible(canvas);
    }

    // Busca el logo en Assets/Resources (como Sprite o como textura)
    static Sprite CargarLogo()
    {
        Sprite sp = Resources.Load<Sprite>("ZPark_Logo");
        if (sp != null) return sp;
        Texture2D tex = Resources.Load<Texture2D>("ZPark_Logo");
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    // Botón del menú: caja de neón con ícono a la izquierda y texto
    RectTransform CrearBotonMenu(string nombre, Transform padre, Vector2 pos, Vector2 tam, Sprite icono,
                                 string titulo, string sub, int tamTexto, Color color, System.Action accion,
                                 out Image borde)
    {
        RectTransform rt = CrearCaja(nombre, padre, pos, tam, color, out borde);
        Image fondo = rt.GetComponent<Image>();
        fondo.raycastTarget = true;
        fondo.color = new Color(0.02f, 0.06f, 0.08f, 0.92f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => accion());

        float x0 = -tam.x * 0.5f + tam.y * 0.6f;
        Image ic = CrearImagen("Icono", rt, icono, color);
        CentrarTam(ic.rectTransform, tam.y * 0.5f, new Vector2(x0, 0));

        Text t = CrearTexto("Texto", rt, new Vector2(0.5f, 0.5f), new Vector2(tam.y * 0.35f, 0),
                            new Vector2(tam.x - tam.y, tam.y), TextAnchor.MiddleCenter, tamTexto, Color.white);
        t.text = string.IsNullOrEmpty(sub) ? titulo : titulo + "\n<size=" + (tamTexto / 2) + "><color=#" +
                 ColorUtility.ToHtmlStringRGB(color) + ">" + sub + "</color></size>";
        return rt;
    }

    // Recuadro informativo de la parte de abajo del menú
    void CrearChip(Transform padre, Vector2 pos, Sprite icono, string titulo, string sub)
    {
        RectTransform rt = CrearCaja("Chip " + titulo, padre, pos, new Vector2(560, 95), ConAlfa(Azul, 0.45f),
                                     out _, new Vector2(0.5f, 0f));
        rt.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        Image ic = CrearImagen("Icono", rt, icono, Color.white);
        CentrarTam(ic.rectTransform, 50, new Vector2(-220, 0));
        Text t = CrearTexto("Texto", rt, new Vector2(0.5f, 0.5f), new Vector2(40, 0), new Vector2(440, 90),
                            TextAnchor.MiddleLeft, 26, Color.white);
        t.text = titulo + "\n<size=21><color=#9FD8F0>" + sub + "</color></size>";
    }

    // Tienda vista desde el menú: muestra las mejoras (se compran durante la partida)
    void AbrirTiendaMenu()
    {
        Sonar(sAbrir);
        tiendaSub.text = "GANA PUNTOS EN LA PARTIDA Y COMPRA AQUÍ TUS MEJORAS";
        tienda.transform.SetAsLastSibling();
        tienda.SetActive(true);
    }

    // ---------- Barra de vida ----------

    void CrearBarraVida(Text texto)
    {
        Transform panel = texto.transform.parent;

        // El texto sube y queda más pequeño; abajo va la barra
        texto.fontSize = 30;
        texto.rectTransform.anchoredPosition = new Vector2(24, 16);

        Image corazon = CrearImagen("Corazon", panel, spCorazon, Rojo);
        CentrarTam(corazon.rectTransform, 50, new Vector2(-150, 0));
        corazonRT = corazon.rectTransform;

        Image fondoBarra = CrearImagen("Barra Fondo", panel, spBlanco, new Color(1f, 1f, 1f, 0.15f));
        RectTransform rf = fondoBarra.rectTransform;
        rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0.5f, 0.5f);
        rf.sizeDelta = new Vector2(270, 18);
        rf.anchoredPosition = new Vector2(30, -20);

        barraVida = CrearImagen("Barra", fondoBarra.transform, spBlanco, Verde);
        barraVida.type = Image.Type.Filled;
        barraVida.fillMethod = Image.FillMethod.Horizontal;
        barraVida.fillOrigin = (int)Image.OriginHorizontal.Left;
        barraVida.fillAmount = 1f;
    }

    void ActualizarBarraVida(GameManager gm)
    {
        float v = gm.pvMax > 0f ? Mathf.Clamp01(gm.PV / gm.pvMax) : 0f;
        barraVida.fillAmount = Mathf.MoveTowards(barraVida.fillAmount, v, Time.unscaledDeltaTime * 1.5f);
        barraVida.color = v > 0.5f ? Color.Lerp(Ambar, Verde, (v - 0.5f) * 2f) : Color.Lerp(Rojo, Ambar, v * 2f);
        textoPV.color = barraVida.color;

        // El corazón late al recibir golpes y más rápido con poca vida
        latidoGolpe = Mathf.MoveTowards(latidoGolpe, 0f, Time.unscaledDeltaTime * 3f);
        float pulso = v <= 0.3f && gm.JuegoActivo ? 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 0f;
        corazonRT.localScale = Vector3.one * (1f + 0.35f * latidoGolpe + pulso);
    }

    // ---------- Flechas de zombies fuera de pantalla ----------

    void CrearFlechas(Transform canvas)
    {
        GameObject raiz = new GameObject("Flechas", typeof(RectTransform));
        raiz.transform.SetParent(canvas, false);
        Estirar((RectTransform)raiz.transform);
        for (int i = 0; i < MaxFlechas; i++)
        {
            Image f = CrearImagen("Flecha " + i, raiz.transform, spFlecha, Rojo);
            f.rectTransform.anchorMin = f.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            f.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            f.rectTransform.sizeDelta = new Vector2(70, 70);
            f.enabled = false;
            flechas[i] = f;
        }
    }

    void ActualizarFlechas(bool jugando)
    {
        Camera cam = Camera.main;
        int usadas = 0;

        if (jugando && cam != null && canvasRT != null)
        {
            // Los más cercanos primero
            List<Zombie> fuera = new List<Zombie>();
            foreach (Zombie z in Zombie.Todos)
            {
                if (z == null || z.Muerto) continue;
                Vector3 vp = cam.WorldToViewportPoint(z.transform.position + Vector3.up * 0.9f);
                bool visible = vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
                if (!visible) fuera.Add(z);
            }
            Vector3 yo = cam.transform.position;
            fuera.Sort((a, b) => (a.transform.position - yo).sqrMagnitude.CompareTo((b.transform.position - yo).sqrMagnitude));

            Vector2 medio = canvasRT.rect.size * 0.5f;
            for (int i = 0; i < fuera.Count && usadas < MaxFlechas; i++)
            {
                Zombie z = fuera[i];
                Vector3 vp = cam.WorldToViewportPoint(z.transform.position + Vector3.up * 0.9f);
                Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (vp.z < 0f) dir = -dir;                       // está detrás: se invierte
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
                dir = new Vector2(dir.x * medio.x, dir.y * medio.y).normalized;

                // Punto en el borde de la pantalla, dejando margen para la barra superior y los botones
                float mx = medio.x - 70f, my = medio.y - 140f;
                float t = Mathf.Min(Mathf.Abs(dir.x) > 0.001f ? mx / Mathf.Abs(dir.x) : float.MaxValue,
                                    Mathf.Abs(dir.y) > 0.001f ? my / Mathf.Abs(dir.y) : float.MaxValue);

                float dist = Vector3.Distance(new Vector3(yo.x, 0f, yo.z),
                                              new Vector3(z.transform.position.x, 0f, z.transform.position.z));
                float cerca = Mathf.Clamp01(1f - (dist - 1f) / 6f);   // 1 = a 1 m, 0 = a 7 m o más

                Image f = flechas[usadas++];
                f.enabled = true;
                f.rectTransform.anchoredPosition = dir * t;
                f.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                float pulso = cerca > 0.6f ? 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) : 0f;
                f.rectTransform.localScale = Vector3.one * (0.7f + 0.6f * cerca + pulso);
                f.color = ConAlfa(Rojo, 0.45f + 0.55f * cerca);
            }
        }

        for (int i = usadas; i < MaxFlechas; i++) flechas[i].enabled = false;
    }

    // ---------- Pausa ----------

    bool pausaAbierta;

    void CrearBotonPausa(Transform padre)
    {
        RectTransform rt = CrearCaja("Boton Pausa", padre, new Vector2(-210, -75), new Vector2(90, 90),
                                     ConAlfa(Color.white, 0.6f), out _, new Vector2(0.5f, 1f));
        Image fondo = rt.GetComponent<Image>();
        fondo.raycastTarget = true;
        fondo.color = new Color(0f, 0f, 0f, 0.6f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(AbrirPausa);
        Image ic = CrearImagen("Icono", rt, spPausa, Color.white);
        CentrarTam(ic.rectTransform, 44, Vector2.zero);
    }

    void CrearPausa(Transform canvas)
    {
        pantallaPausa = new GameObject("Pausa", typeof(RectTransform));
        pantallaPausa.transform.SetParent(canvas, false);
        Estirar((RectTransform)pantallaPausa.transform);
        Image oscuro = pantallaPausa.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.65f);
        oscuro.raycastTarget = true;

        RectTransform panel = CrearCaja("Panel", pantallaPausa.transform, Vector2.zero, new Vector2(720, 520), Azul, out _);
        CrearTexto("Titulo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 175), new Vector2(700, 90),
                   TextAnchor.MiddleCenter, 70, Azul).text = "PAUSA";
        CrearBotonCaja("Continuar", panel, new Vector2(0, 30), new Vector2(520, 110), "CONTINUAR", 48, Verde, CerrarPausa);
        CrearBotonCaja("Salir", panel, new Vector2(0, -120), new Vector2(520, 95), "SALIR AL MENÚ", 38, Rojo, () =>
        {
            Sonar(sAbrir);
            if (GameManager.Instancia != null) GameManager.Instancia.IrAlMenu();
        });
        CrearTexto("Nota", panel, new Vector2(0.5f, 0.5f), new Vector2(0, -205), new Vector2(700, 40),
                   TextAnchor.MiddleCenter, 22, new Color(0.75f, 0.75f, 0.75f)).text =
                   "Si sales, esta partida no se guarda en el ranking";
        pantallaPausa.SetActive(false);
    }

    void AbrirPausa()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null || !gm.JuegoActivo) return;
        Sonar(sAbrir);
        gm.Pausar(true);
        pausaAbierta = true;
        pantallaPausa.transform.SetAsLastSibling();
        pantallaPausa.SetActive(true);
    }

    void CerrarPausa()
    {
        Sonar(sAbrir);
        pausaAbierta = false;
        pantallaPausa.SetActive(false);
        if (GameManager.Instancia != null) GameManager.Instancia.Pausar(false);
    }

    // Si el jugador sale de la app (llamada, botón de inicio), la partida se pausa sola
    void OnApplicationPause(bool enPausa)
    {
        if (enPausa && pantallaPausa != null) AbrirPausa();
    }

    // ---------- Cómo jugar ----------

    void CrearTutorial(Transform canvas)
    {
        tutorial = new GameObject("Como jugar", typeof(RectTransform));
        tutorial.transform.SetParent(canvas, false);
        Estirar((RectTransform)tutorial.transform);
        Image oscuro = tutorial.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.75f);
        oscuro.raycastTarget = true;

        RectTransform panel = CrearCaja("Panel", tutorial.transform, Vector2.zero, new Vector2(1720, 860), Verde, out _);
        CrearTexto("Titulo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 360), new Vector2(1600, 90),
                   TextAnchor.MiddleCenter, 66, Verde).text = "CÓMO JUGAR";

        CrearPasoTutorial(panel, new Vector2(-545, 40), "1", spMira, Verde, "ESCANEA Y COLOCA",
                          "Apunta al piso y mueve el\ncelular despacio. Cuando\naparezca el círculo, toca\nla pantalla para colocar\nel campo de batalla.");
        CrearPasoTutorial(panel, new Vector2(0, 40), "2", spBala, RojoBrillo, "ATACA Y DEFIÉNDETE",
                          "Pon la mira + sobre un\nzombie y mantén ATACAR.\nDEFENDER te protege 2 s,\npero luego debe recargar.");
        CrearPasoTutorial(panel, new Vector2(545, 40), "3", spCarrito, Azul, "MEJORA Y SOBREVIVE",
                          "Gasta tus puntos en la\nTIENDA. Sobrevive 5 minutos\ny 6 oleadas para ganar.");

        CrearTexto("Consejo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, -255), new Vector2(1600, 44),
                   TextAnchor.MiddleCenter, 28, Ambar).text =
                   "Las flechas rojas en los bordes te avisan de los zombies que vienen por detrás o por los lados";

        RectTransform boton = CrearBotonCaja("Entendido", panel, new Vector2(0, -345), new Vector2(560, 105), "", 48, Verde, () =>
        {
            Sonar(sAbrir);
            PlayerPrefs.SetInt("zpark_tutorial", 1);
            PlayerPrefs.Save();
            tutorial.SetActive(false);
            if (tutorialDesdeJugar && GameManager.Instancia != null) GameManager.Instancia.EmpezarPartida();
        });
        tutorialBoton = boton.GetComponentInChildren<Text>();
        tutorial.SetActive(false);
    }

    void CrearPasoTutorial(Transform padre, Vector2 pos, string num, Sprite icono, Color color, string titulo, string texto)
    {
        RectTransform t = CrearCaja("Paso " + num, padre, pos, new Vector2(500, 500), color, out _);

        Image bola = CrearImagen("Esfera", t, spEsfera, new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f));
        CentrarTam(bola.rectTransform, 130, new Vector2(0, 165));
        Image aro = CrearImagen("Anillo", t, spAnillo, color);
        CentrarTam(aro.rectTransform, 130, new Vector2(0, 165));
        Image ic = CrearImagen("Icono", t, icono, color);
        CentrarTam(ic.rectTransform, 74, new Vector2(0, 165));
        CrearTexto("Numero", t, new Vector2(0.5f, 0.5f), new Vector2(-200, 210), new Vector2(80, 60),
                   TextAnchor.MiddleCenter, 44, color).text = num;

        CrearTexto("Titulo", t, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(480, 50),
                   TextAnchor.MiddleCenter, 34, color).text = titulo;
        CrearTexto("Texto", t, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(470, 240),
                   TextAnchor.MiddleCenter, 28, Color.white).text = texto;
    }

    void MostrarTutorial(bool desdeJugar)
    {
        tutorialDesdeJugar = desdeJugar;
        tutorialBoton.text = desdeJugar ? "¡A JUGAR!" : "ENTENDIDO";
        tutorial.transform.SetAsLastSibling();
        tutorial.SetActive(true);
    }

    // ---------- Celular no compatible con ARCore ----------

    void CrearNoCompatible(Transform canvas)
    {
        noCompatible = new GameObject("No compatible", typeof(RectTransform));
        noCompatible.transform.SetParent(canvas, false);
        Estirar((RectTransform)noCompatible.transform);
        Image fondo = noCompatible.AddComponent<Image>();
        fondo.color = new Color(0.01f, 0.03f, 0.04f, 1f);
        fondo.raycastTarget = true;

        RectTransform panel = CrearCaja("Panel", noCompatible.transform, Vector2.zero, new Vector2(1300, 640), Ambar, out _);
        CrearTexto("Titulo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 210), new Vector2(1250, 90),
                   TextAnchor.MiddleCenter, 60, Ambar).text = "CELULAR NO COMPATIBLE";
        CrearTexto("Texto", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1200, 200),
                   TextAnchor.MiddleCenter, 36, Color.white).text =
                   "Z-PARK usa la realidad aumentada de Google (ARCore)\n" +
                   "y este celular no la tiene disponible.\n" +
                   "<size=28><color=#BBBBBB>Revisa si tu modelo aparece en la lista de celulares compatibles.</color></size>";
        CrearBotonCaja("Ver lista", panel, new Vector2(-200, -200), new Vector2(520, 100), "VER CELULARES COMPATIBLES", 32, Verde,
                       () => Application.OpenURL("https://developers.google.com/ar/devices?hl=es-419"));
        CrearBotonCaja("Salir", panel, new Vector2(270, -200), new Vector2(300, 100), "SALIR", 36, Rojo,
                       () => Application.Quit());
        noCompatible.SetActive(false);
    }

    // ---------- Ajustes (sonido y vibración) ----------

    void CrearAjustes(Transform canvas)
    {
        ajustes = new GameObject("Ajustes", typeof(RectTransform));
        ajustes.transform.SetParent(canvas, false);
        Estirar((RectTransform)ajustes.transform);
        Image oscuro = ajustes.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.7f);
        oscuro.raycastTarget = true;

        RectTransform panel = CrearCaja("Panel", ajustes.transform, Vector2.zero, new Vector2(720, 640), Azul, out _);
        CrearTexto("Titulo", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 245), new Vector2(700, 80),
                   TextAnchor.MiddleCenter, 56, Azul).text = "AJUSTES";

        textoSonido = CrearBotonCaja("Sonido", panel, new Vector2(0, 130), new Vector2(520, 95), "", 38, Verde, () =>
        {
            sonidoOn = !sonidoOn;
            PlayerPrefs.SetInt("zpark_sonido", sonidoOn ? 1 : 0);
            PlayerPrefs.Save();
            AplicarAjustes();
            Sonar(sAbrir);
        }).GetComponentInChildren<Text>();

        textoVibra = CrearBotonCaja("Vibracion", panel, new Vector2(0, 15), new Vector2(520, 95), "", 38, Verde, () =>
        {
            vibraOn = !vibraOn;
            PlayerPrefs.SetInt("zpark_vibra", vibraOn ? 1 : 0);
            PlayerPrefs.Save();
            AplicarAjustes();
            Sonar(sAbrir);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (vibraOn) Handheld.Vibrate();
#endif
        }).GetComponentInChildren<Text>();

        CrearBotonCaja("Como jugar", panel, new Vector2(0, -100), new Vector2(520, 95), "CÓMO JUGAR", 38, Azul, () =>
        {
            Sonar(sAbrir);
            ajustes.SetActive(false);
            MostrarTutorial(false);
        });

        CrearBotonCaja("Cerrar", panel, new Vector2(0, -225), new Vector2(320, 85), "CERRAR", 36, Ambar, () =>
        {
            Sonar(sAbrir);
            ajustes.SetActive(false);
        });

        sonidoOn = PlayerPrefs.GetInt("zpark_sonido", 1) == 1;
        vibraOn = PlayerPrefs.GetInt("zpark_vibra", 1) == 1;
        AplicarAjustes();
        ajustes.SetActive(false);
    }

    void AplicarAjustes()
    {
        AudioListener.volume = sonidoOn ? 1f : 0f;
        if (textoSonido != null) textoSonido.text = "SONIDO:  " + (sonidoOn ? "SÍ" : "NO");
        if (textoVibra != null) textoVibra.text = "VIBRACIÓN:  " + (vibraOn ? "SÍ" : "NO");
    }

    void AlTocarJugar()
    {
        Sonar(sAbrir);
        string alias = campoAlias.text.Trim().ToUpperInvariant();
        if (alias.Length == 0) alias = "JUGADOR";
        Ranking.Alias = alias;

        // La primera vez se explica cómo jugar
        if (PlayerPrefs.GetInt("zpark_tutorial", 0) == 0)
        {
            MostrarTutorial(true);
            return;
        }
        if (GameManager.Instancia != null) GameManager.Instancia.EmpezarPartida();
    }

    InputField CrearCampoTexto(Transform padre, Vector2 pos, Vector2 tam)
    {
        RectTransform caja = CrearCaja("Campo Alias", padre, pos, tam, Color.white, out _);
        Image fondo = caja.GetComponent<Image>();
        fondo.raycastTarget = true;

        Text texto = CrearTexto("Texto", caja, new Vector2(0.5f, 0.5f), Vector2.zero,
                                new Vector2(tam.x - 40, tam.y - 10), TextAnchor.MiddleCenter, 44, Color.white);
        texto.supportRichText = false;
        Text guia = CrearTexto("Guia", caja, new Vector2(0.5f, 0.5f), Vector2.zero,
                               new Vector2(tam.x - 40, tam.y - 10), TextAnchor.MiddleCenter, 36,
                               new Color(0.6f, 0.6f, 0.6f));
        guia.fontStyle = FontStyle.Italic;
        guia.text = "Toca para escribir";

        InputField campo = caja.gameObject.AddComponent<InputField>();
        campo.targetGraphic = fondo;
        campo.transition = Selectable.Transition.None;
        campo.textComponent = texto;
        campo.placeholder = guia;
        campo.characterLimit = 12;
        campo.lineType = InputField.LineType.SingleLine;
        campo.contentType = InputField.ContentType.Alphanumeric;
        return campo;
    }

    // ---------- Ranking ----------

    static readonly string[] ColumnasRanking = { "#", "ALIAS", "PUNTOS", "ZOMBIES", "TIEMPO", "OLEADAS", "FECHA" };
    static readonly float[] XColumnas = { -445f, -300f, -120f, 30f, 165f, 300f, 435f };

    void CrearRanking(Transform canvas)
    {
        pantallaRanking = new GameObject("Ranking", typeof(RectTransform));
        pantallaRanking.transform.SetParent(canvas, false);
        Estirar((RectTransform)pantallaRanking.transform);
        Image oscuro = pantallaRanking.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.75f);
        oscuro.raycastTarget = true;

        // Volver
        RectTransform volver = CrearCaja("Volver", pantallaRanking.transform, new Vector2(150, -70),
                                         new Vector2(230, 85), Verde, out _, new Vector2(0f, 1f));
        Image fv = volver.GetComponent<Image>();
        fv.raycastTarget = true;
        Button bv = volver.gameObject.AddComponent<Button>();
        bv.transition = Selectable.Transition.None;
        bv.onClick.AddListener(() => { Sonar(sAbrir); rankingAbierto = false; });
        CrearTexto("Texto", volver, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 85),
                   TextAnchor.MiddleCenter, 36, Verde).text = "< VOLVER";

        // Título
        CrearTexto("Titulo", pantallaRanking.transform, new Vector2(0.5f, 1f), new Vector2(0, -35),
                   new Vector2(1000, 100), TextAnchor.MiddleCenter, 80, Verde).text = "RANKING";
        CrearTexto("Subtitulo", pantallaRanking.transform, new Vector2(0.5f, 1f), new Vector2(0, -130),
                   new Vector2(1000, 40), TextAnchor.MiddleCenter, 26, new Color(0.8f, 0.8f, 0.8f)).text =
                   "TUS MEJORES PUNTAJES · GUARDADO EN ESTE DISPOSITIVO";

        // Tarjeta del mejor puntaje (izquierda)
        RectTransform mejor = CrearCaja("Mejor", pantallaRanking.transform, new Vector2(-640, -90),
                                        new Vector2(520, 600), Ambar, out _);
        Image corona = CrearImagen("Corona", mejor, spCorona, Ambar);
        CentrarTam(corona.rectTransform, 120, new Vector2(0, 205));
        CrearTexto("Etiqueta", mejor, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(500, 40),
                   TextAnchor.MiddleCenter, 30, Ambar).text = "MEJOR PUNTAJE";
        rkMejorPuntos = CrearTexto("Puntos", mejor, new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(500, 120),
                                   TextAnchor.MiddleCenter, 110, Verde);
        rkMejorAlias = CrearTexto("Alias", mejor, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(500, 50),
                                  TextAnchor.MiddleCenter, 42, Color.white);
        rkMejorDetalle = CrearTexto("Detalle", mejor, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(500, 180),
                                    TextAnchor.MiddleCenter, 30, new Color(0.85f, 0.85f, 0.85f));

        // Tabla (derecha)
        RectTransform tabla = CrearCaja("Tabla", pantallaRanking.transform, new Vector2(240, -90),
                                        new Vector2(1060, 600), Azul, out _);
        for (int c = 0; c < ColumnasRanking.Length; c++)
            CrearTexto("Col " + c, tabla, new Vector2(0.5f, 0.5f), new Vector2(XColumnas[c], 250),
                       new Vector2(160, 40), TextAnchor.MiddleCenter, 24, Azul).text = ColumnasRanking[c];

        for (int f = 0; f < FilasRanking; f++)
        {
            GameObject fila = new GameObject("Fila " + (f + 1), typeof(RectTransform));
            fila.transform.SetParent(tabla, false);
            Image fondo = fila.AddComponent<Image>();
            fondo.color = new Color(1f, 1f, 1f, f % 2 == 0 ? 0.06f : 0.02f);
            fondo.raycastTarget = false;
            RectTransform rf = fondo.rectTransform;
            rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0.5f, 0.5f);
            rf.sizeDelta = new Vector2(1010, 64);
            rf.anchoredPosition = new Vector2(0, 185 - f * 70);
            rkFilas[f] = fila;

            for (int c = 0; c < ColumnasRanking.Length; c++)
            {
                int tam = c == 2 ? 34 : (c == 6 ? 22 : 28);
                Color col = c == 2 ? Verde : Color.white;
                rkCeldas[f, c] = CrearTexto("Celda", fila.transform, new Vector2(0.5f, 0.5f),
                                            new Vector2(XColumnas[c], 0), new Vector2(c == 1 ? 230 : 160, 60),
                                            TextAnchor.MiddleCenter, tam, col);
            }
        }

        rkVacio = CrearTexto("Vacio", tabla, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 120),
                             TextAnchor.MiddleCenter, 36, new Color(0.8f, 0.8f, 0.8f));
        rkVacio.text = "Aún no hay partidas guardadas.\n¡Juega la primera!";

        pantallaRanking.SetActive(false);
    }

    void RefrescarRanking()
    {
        List<Partida> lista = Ranking.Cargar();
        rkVacio.gameObject.SetActive(lista.Count == 0);

        if (lista.Count > 0)
        {
            Partida m = lista[0];
            rkMejorPuntos.text = m.puntaje.ToString();
            rkMejorAlias.text = m.alias;
            rkMejorDetalle.text = "Zombies: " + m.zombies +
                                  "\nTiempo: " + MinSeg(m.segundos) +
                                  "\nOleadas: " + m.oleadas + "/" + m.oleadasTotales +
                                  "\n" + (m.victoria ? "<color=#A6E32A>SOBREVIVIÓ</color>" : "<color=#F04A3A>INFECTADO</color>");
        }
        else
        {
            rkMejorPuntos.text = "—";
            rkMejorAlias.text = "";
            rkMejorDetalle.text = "";
        }

        for (int f = 0; f < FilasRanking; f++)
        {
            bool hay = f < lista.Count;
            rkFilas[f].SetActive(hay);
            if (!hay) continue;

            Partida p = lista[f];
            rkCeldas[f, 0].text = (f + 1).ToString();
            rkCeldas[f, 0].color = f == 0 ? Ambar : Color.white;
            rkCeldas[f, 1].text = p.alias;
            rkCeldas[f, 2].text = p.puntaje.ToString();
            rkCeldas[f, 3].text = p.zombies.ToString();
            rkCeldas[f, 4].text = MinSeg(p.segundos);
            rkCeldas[f, 5].text = p.oleadas + "/" + p.oleadasTotales;
            rkCeldas[f, 6].text = p.fecha;
        }
    }

    // Caja oscura con esquinas cortadas y borde de neón
    RectTransform CrearCaja(string nombre, Transform padre, Vector2 pos, Vector2 tam, Color colorBorde, out Image borde)
    {
        return CrearCaja(nombre, padre, pos, tam, colorBorde, out borde, new Vector2(0.5f, 0.5f));
    }

    // "ancla" = punto del padre desde donde se mide pos (la caja siempre se centra en pos)
    RectTransform CrearCaja(string nombre, Transform padre, Vector2 pos, Vector2 tam, Color colorBorde,
                            out Image borde, Vector2 ancla)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        Image fondo = go.AddComponent<Image>();
        fondo.sprite = spPanel;
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 0.6f;
        fondo.color = FondoPanel;
        fondo.raycastTarget = false;

        borde = CrearImagen("Borde", go.transform, spBorde, colorBorde);
        borde.type = Image.Type.Sliced;
        borde.pixelsPerUnitMultiplier = 0.6f;
        return rt;
    }

    static Color ConAlfa(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

    // ---------- Construcción de UI ----------

    BotonEsfera CrearBotonEsfera(string nombre, Transform padre, Vector2 ancla, Vector2 pos, float d,
                                 Sprite icono, string texto, Color brillo, Color esfera, Color colorIcono)
    {
        GameObject raiz = new GameObject(nombre, typeof(RectTransform));
        raiz.transform.SetParent(padre, false);
        RectTransform rr = (RectTransform)raiz.transform;
        rr.anchorMin = ancla;
        rr.anchorMax = ancla;
        rr.pivot = ancla;
        rr.anchoredPosition = pos;
        rr.sizeDelta = new Vector2(d, d);

        BotonEsfera be = new BotonEsfera();

        GameObject cuerpo = new GameObject("Cuerpo", typeof(RectTransform));
        cuerpo.transform.SetParent(raiz.transform, false);
        be.cuerpo = (RectTransform)cuerpo.transform;
        Estirar(be.cuerpo);
        Image zona = cuerpo.AddComponent<Image>();
        zona.sprite = spCirculo;
        zona.color = new Color(0f, 0f, 0f, 0f);
        zona.raycastTarget = true;

        be.glow = CrearImagen("Glow", cuerpo.transform, spGlow, ConAlfa(brillo, 0.5f));
        CentrarTam(be.glow.rectTransform, d * 1.45f, Vector2.zero);

        be.segBase = CrearImagen("Segmentos Base", cuerpo.transform, spSegmentos, ConAlfa(brillo, 0.2f));
        be.seg = CrearImagen("Segmentos", cuerpo.transform, spSegmentos, brillo);
        be.seg.type = Image.Type.Filled;
        be.seg.fillMethod = Image.FillMethod.Radial360;
        be.seg.fillOrigin = (int)Image.Origin360.Top;
        be.seg.fillClockwise = true;
        be.seg.fillAmount = 1f;

        be.esfera = CrearImagen("Esfera", cuerpo.transform, spEsfera, esfera);
        CentrarTam(be.esfera.rectTransform, d * 0.8f, Vector2.zero);

        Image anillo = CrearImagen("Anillo", cuerpo.transform, spAnillo, ConAlfa(brillo, 0.9f));
        CentrarTam(anillo.rectTransform, d * 0.8f, Vector2.zero);

        be.brillo = CrearImagen("Brillo", cuerpo.transform, spBrillo, new Color(1f, 1f, 1f, 0.35f));
        CentrarTam(be.brillo.rectTransform, d * 0.8f, Vector2.zero);

        be.iconoPos = new Vector2(0f, d * 0.07f);
        be.icono = CrearImagen("Icono", cuerpo.transform, icono, colorIcono);
        CentrarTam(be.icono.rectTransform, d * 0.36f, be.iconoPos);

        be.etiqueta = CrearTexto("Texto", cuerpo.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -d * 0.2f),
                                 new Vector2(d, 50f), TextAnchor.MiddleCenter, 28, Color.white);
        be.etiqueta.text = texto;
        return be;
    }

    Image CrearImagen(string nombre, Transform padre, Sprite sp, Color c)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sp;
        img.color = c;
        img.raycastTarget = false;
        Estirar(img.rectTransform);
        return img;
    }

    static void CentrarTam(RectTransform rt, float tam, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tam, tam);
        rt.anchoredPosition = pos;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void CrearEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        InputSystemUIInputModule modulo = es.AddComponent<InputSystemUIInputModule>();
        modulo.AssignDefaultActions();
    }

    // Panel oscuro (esquinas cortadas + borde suave del color del texto) con un texto adentro
    Text CrearPanelConTexto(string nombre, Transform padre, Vector2 ancla, Vector2 pos,
                            Vector2 tamano, int tam, Color color)
    {
        GameObject panel = new GameObject(nombre + " Panel", typeof(RectTransform));
        panel.transform.SetParent(padre, false);

        Image img = panel.AddComponent<Image>();
        img.sprite = spPanel;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.2f;
        img.color = Fondo;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;

        Image borde = CrearImagen("Borde", panel.transform, spBorde, ConAlfa(color, 0.55f));
        borde.type = Image.Type.Sliced;
        borde.pixelsPerUnitMultiplier = 1.2f;

        return CrearTexto(nombre, panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                          tamano, TextAnchor.MiddleCenter, tam, color);
    }

    Text CrearTexto(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tamano,
                    TextAnchor alineacion, int tam, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tam;
        t.fontStyle = FontStyle.Bold;
        t.alignment = alineacion;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;

        Outline borde = go.AddComponent<Outline>();
        borde.effectColor = Color.black;
        borde.effectDistance = new Vector2(3, -3);

        RectTransform rt = t.rectTransform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;
        return t;
    }

    // ---------- Sprites dibujados por código ----------

    void CrearSprites()
    {
        spCirculo = HacerSprite(64, (x, y, p) => Borde(0.97f - R(x, y), p), null);

        spEsfera = HacerSprite(256, (x, y, p) => Borde(0.97f - R(x, y), p),
            (x, y) =>
            {
                float r = R(x, y);
                return Mathf.Lerp(1f, 0.45f, r * r) * (0.85f + 0.15f * y);
            });

        spAnillo = HacerSprite(256, (x, y, p) =>
        {
            float r = R(x, y);
            return Mathf.Min(Borde(0.97f - r, p), Borde(r - 0.91f, p));
        }, null);

        spBrillo = HacerSprite(128, (x, y, p) =>
        {
            float ex = x / 0.62f, ey = (y - 0.45f) / 0.32f;
            float e = Mathf.Sqrt(ex * ex + ey * ey);
            return Mathf.Clamp01(1f - e) * Mathf.Clamp01((y - 0.1f) * 3f);
        }, null);

        spGlow = HacerSprite(128, (x, y, p) =>
        {
            float r = R(x, y);
            float t = (r - 0.6f) / 0.16f;
            return Mathf.Exp(-t * t) * Mathf.Clamp01((0.98f - r) * 10f);
        }, null);

        spSegmentos = HacerSprite(256, (x, y, p) =>
        {
            float r = R(x, y);
            float anillo = Mathf.Min(Borde(0.97f - r, p), Borde(r - 0.88f, p));
            const int n = 36;
            float ang = Mathf.Atan2(y, x) / (2f * Mathf.PI);
            float s = Mathf.Repeat(ang * n, 1f);
            float dAng = Mathf.Min(s - 0.15f, 0.85f - s) * (2f * Mathf.PI * r / n);
            return anillo * Borde(dAng, p);
        }, null);

        spBala = HacerSprite(256, (x, y, p) =>
        {
            float u = (x - y) * 0.7071f;
            float v = (x + y) * 0.7071f;
            float au = Mathf.Abs(u);
            float casquillo = Mathf.Min(0.26f - au, Mathf.Min(v + 0.85f, -0.12f - v));
            float aro = Mathf.Min(0.31f - au, Mathf.Min(v + 0.85f, -0.72f - v));
            float w = v < 0.3f ? 0.24f : 0.24f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((v - 0.3f) / 0.55f, 2f)));
            float punta = Mathf.Min(w - au, Mathf.Min(v + 0.04f, 0.85f - v));
            return Mathf.Max(Borde(casquillo, p), Mathf.Max(Borde(aro, p), Borde(punta, p)));
        }, null);

        spEscudo = HacerSprite(256, (x, y, p) =>
        {
            float ax = Mathf.Abs(x);
            float ancho = y >= 0f ? 0.75f : 0.75f * (1f - Mathf.Pow(Mathf.Clamp01(-y / 0.9f), 1.6f));
            float dist = Mathf.Min(ancho - ax, Mathf.Min(0.8f - y, y + 0.9f));
            float ext = Borde(dist, p);
            float in1 = Borde(dist - 0.1f, p);
            float in2 = Borde(dist - 0.18f, p);
            return ext * (1f - in1) + in1 * (1f - in2) * 0.45f + in2 * 0.85f;
        }, null);

        // Viñeta: transparente al centro, opaca hacia los bordes
        spVineta = HacerSprite(128, (x, y, p) =>
        {
            float d = Mathf.Sqrt(x * x + y * y) / 1.414f;
            return Mathf.Pow(Mathf.Clamp01((d - 0.45f) / 0.55f), 1.5f);
        }, null);

        // ---- Íconos del menú ----
        spMira = HacerSprite(128, (x, y, p) =>
        {
            float r = R(x, y), ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float circ = Mathf.Min(Borde(0.62f - r, p), Borde(r - 0.48f, p));
            float vert = Mathf.Min(Borde(0.08f - ax, p), Mathf.Min(Borde(ay - 0.3f, p), Borde(0.95f - ay, p)));
            float hori = Mathf.Min(Borde(0.08f - ay, p), Mathf.Min(Borde(ax - 0.3f, p), Borde(0.95f - ax, p)));
            return Mathf.Max(Mathf.Max(circ, Borde(0.13f - r, p)), Mathf.Max(vert, hori));
        }, null);

        spPlay = HacerSprite(128, (x, y, p) => Borde(Mathf.Min(Lado(x, y, -0.6f, -0.8f, 0.85f, 0f),
            Mathf.Min(Lado(x, y, 0.85f, 0f, -0.6f, 0.8f), Lado(x, y, -0.6f, 0.8f, -0.6f, -0.8f))), p), null);

        spTrofeo = HacerSprite(128, (x, y, p) =>
        {
            float ax = Mathf.Abs(x);
            float copa = Mathf.Max(Caja(x, y, 0f, 0.66f, 0.5f, 0.16f),
                                   Mathf.Min(0.5f - R(x, y - 0.45f), 0.66f - y));
            float da = R(ax - 0.55f, y - 0.45f);
            float asas = Mathf.Min(Mathf.Min(0.24f - da, da - 0.14f), ax - 0.45f);
            float tallo = Caja(x, y, 0f, -0.2f, 0.09f, 0.25f);
            float base1 = Caja(x, y, 0f, -0.52f, 0.36f, 0.09f);
            float base2 = Caja(x, y, 0f, -0.72f, 0.5f, 0.1f);
            float d = Mathf.Max(Mathf.Max(copa, asas), Mathf.Max(tallo, Mathf.Max(base1, base2)));
            return Borde(d, p);
        }, null);

        spCarrito = HacerSprite(128, (x, y, p) =>
        {
            float cesta = Mathf.Min(Caja(x, y, 0.12f, 0.2f, 0.62f, 0.34f), -Caja(x, y, 0.12f, 0.2f, 0.48f, 0.2f));
            float asa = Seg(x, y, -0.92f, 0.7f, -0.5f, 0.54f, 0.08f);
            float barra = Caja(x, y, 0.12f, -0.3f, 0.6f, 0.06f);
            float rueda1 = 0.14f - R(x + 0.25f, y + 0.6f);
            float rueda2 = 0.14f - R(x - 0.5f, y + 0.6f);
            float d = Mathf.Max(Mathf.Max(cesta, asa), Mathf.Max(barra, Mathf.Max(rueda1, rueda2)));
            return Borde(d, p);
        }, null);

        spEngranaje = HacerSprite(128, (x, y, p) =>
        {
            float r = R(x, y);
            float dientes = Mathf.Clamp01((Mathf.Cos(8f * Mathf.Atan2(y, x)) - 0.1f) * 3f);
            float lim = 0.62f + 0.24f * dientes;
            return Mathf.Min(Borde(lim - r, p), Borde(r - 0.27f, p));
        }, null);

        spCelular = HacerSprite(128, (x, y, p) =>
            Borde(Mathf.Min(Caja(x, y, 0f, 0f, 0.45f, 0.85f), -Caja(x, y, 0f, 0.04f, 0.33f, 0.64f)), p), null);

        spCorazon = HacerSprite(128, (x, y, p) =>
        {
            float c1 = 0.4f - R(x + 0.33f, y - 0.3f);
            float c2 = 0.4f - R(x - 0.33f, y - 0.3f);
            float pico = Mathf.Min(Lado(x, y, -0.64f, 0.12f, 0f, -0.78f),
                         Mathf.Min(Lado(x, y, 0f, -0.78f, 0.64f, 0.12f), Lado(x, y, 0.64f, 0.12f, -0.64f, 0.12f) + 0.15f));
            return Borde(Mathf.Max(Mathf.Max(c1, c2), pico), p);
        }, null);

        spBlanco = HacerSprite(8, (x, y, p) => 1f, null);

        // Flecha que apunta a la derecha (se gira hacia el zombie)
        spFlecha = HacerSprite(128, (x, y, p) =>
        {
            // Triángulo con una muesca atrás (forma de punta de flecha)
            float tri = Mathf.Min(Lado(x, y, -0.7f, -0.75f, 0.85f, 0f),
                        Mathf.Min(Lado(x, y, 0.85f, 0f, -0.7f, 0.75f), Lado(x, y, -0.7f, 0.75f, -0.7f, -0.75f)));
            float fueraMuesca = Mathf.Max(Lado(x, y, -0.7f, 0.75f, -0.3f, 0f), Lado(x, y, -0.3f, 0f, -0.7f, -0.75f));
            return Borde(Mathf.Min(tri, fueraMuesca), p);
        }, null);

        // Pausa: dos barras
        spPausa = HacerSprite(64, (x, y, p) =>
            Borde(Mathf.Max(Caja(x, y, -0.3f, 0f, 0.16f, 0.7f), Caja(x, y, 0.3f, 0f, 0.16f, 0.7f)), p), null);

        // Corona (mejor puntaje)
        spCorona = HacerSprite(128, (x, y, p) =>
        {
            float ax = Mathf.Abs(x);
            float dPico = Mathf.Min(Mathf.Abs(x + 0.75f), Mathf.Min(ax, Mathf.Abs(x - 0.75f)));
            float techo = 0.6f - 0.55f * Mathf.Min(dPico / 0.375f, 1f);
            float cuerpo = Mathf.Min(0.8f - ax, Mathf.Min(y + 0.6f, (techo - y) * 0.6f));
            float a = Borde(cuerpo, p) * (1f - 0.6f * Borde(0.04f - Mathf.Abs(y + 0.3f), p)); // línea de la base
            for (int k = -1; k <= 1; k++)
            {
                float dx = x - k * 0.75f, dy = y - 0.7f;
                a = Mathf.Max(a, Borde(0.11f - Mathf.Sqrt(dx * dx + dy * dy), p));
            }
            return a;
        }, null);

        // Estallido (ícono de Potencia)
        spEstallido = HacerSprite(256, (x, y, p) =>
        {
            float r = R(x, y);
            float c = 0.5f + 0.5f * Mathf.Cos(8f * Mathf.Atan2(y, x));
            float lim = 0.42f + 0.5f * c * c * c * c;
            return Borde(lim - r, p);
        }, null);

        // Tres balas verticales (ícono de Cadencia)
        spBalas = HacerSprite(256, (x, y, p) =>
        {
            float a = 0f;
            for (int k = -1; k <= 1; k++)
            {
                float au = Mathf.Abs(x - k * 0.45f);
                float casquillo = Mathf.Min(0.15f - au, Mathf.Min(y + 0.85f, -0.08f - y));
                float w = y < 0.25f ? 0.14f : 0.14f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((y - 0.25f) / 0.55f, 2f)));
                float punta = Mathf.Min(w - au, Mathf.Min(y + 0.02f, 0.8f - y));
                a = Mathf.Max(a, Mathf.Max(Borde(casquillo, p), Borde(punta, p)));
            }
            return a;
        }, null);

        // Panel con esquinas cortadas (relleno) y su borde de neón. Se estiran con 9-slice.
        Vector4 corte = new Vector4(40, 40, 40, 40);
        spPanel = HacerSprite(128, (x, y, p) => Borde(DistChaflan(x, y), p), null, corte);
        spBorde = HacerSprite(128, (x, y, p) =>
        {
            float dist = DistChaflan(x, y);
            return Mathf.Min(Borde(dist, p), Borde(0.05f - dist, p));
        }, null, corte);
    }

    // Distancia al borde de un cuadrado con las 4 esquinas cortadas en diagonal
    static float DistChaflan(float x, float y)
    {
        float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
        const float k = 0.38f;
        return Mathf.Min(Mathf.Min(0.97f - ax, 0.97f - ay), ((1.94f - k) - (ax + ay)) * 0.7071f);
    }

    static float R(float x, float y) { return Mathf.Sqrt(x * x + y * y); }

    // Distancia (positiva adentro) a una caja centrada en (cx, cy) con medio ancho/alto hw, hh
    static float Caja(float x, float y, float cx, float cy, float hw, float hh)
    {
        return Mathf.Min(hw - Mathf.Abs(x - cx), hh - Mathf.Abs(y - cy));
    }

    // Lado de un triángulo recorrido en sentido antihorario (positivo adentro)
    static float Lado(float x, float y, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        return (dx * (y - ay) - dy * (x - ax)) / Mathf.Sqrt(dx * dx + dy * dy);
    }

    // Segmento grueso de (ax, ay) a (bx, by)
    static float Seg(float x, float y, float ax, float ay, float bx, float by, float grosor)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
        return grosor - R(x - (ax + t * dx), y - (ay + t * dy));
    }

    static float Borde(float d, float p) { return Mathf.Clamp01(d / p + 0.5f); }

    static Sprite HacerSprite(int tam, System.Func<float, float, float, float> alfa,
                              System.Func<float, float, float> luz)
    {
        return HacerSprite(tam, alfa, luz, Vector4.zero);
    }

    // alfa(x, y, tamañoPixel) da la forma; luz(x, y) opcional da el degradado; borde9 = 9-slice
    static Sprite HacerSprite(int tam, System.Func<float, float, float, float> alfa,
                              System.Func<float, float, float> luz, Vector4 borde9)
    {
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pix = new Color32[tam * tam];
        float p = 2f / tam;
        for (int j = 0; j < tam; j++)
        {
            float y = (j + 0.5f) / tam * 2f - 1f;
            for (int i = 0; i < tam; i++)
            {
                float x = (i + 0.5f) / tam * 2f - 1f;
                byte a = (byte)(Mathf.Clamp01(alfa(x, y, p)) * 255f);
                byte l = (byte)(Mathf.Clamp01(luz != null ? luz(x, y) : 1f) * 255f);
                pix[j * tam + i] = new Color32(l, l, l, a);
            }
        }
        tex.SetPixels32(pix);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f,
                             0, SpriteMeshType.FullRect, borde9);
    }
}
