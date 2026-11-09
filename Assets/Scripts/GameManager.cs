using UnityEngine;
using UnityEngine.SceneManagement;

public enum EstadoPartida { Jugando, Victoria, Derrota, Preparando, Menu }

public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    // true = al recargar la escena se salta el menú (botón JUGAR DE NUEVO)
    static bool saltarMenu;

    [Header("Arena")]
    [Tooltip("Escanear el suelo y colocar la arena antes de empezar")]
    public bool usarArena = true;

    [Header("Jugador")]
    public float pvMax = 100f;

    [Header("Escudo (botón DEFENDER)")]
    public float duracionEscudo = 2f;   // segundos sin recibir daño
    public float cooldownEscudo = 8f;   // espera entre usos (cuenta desde que se activa)

    [Header("Partida")]
    public float duracionPartida = 300f; // 5:00
    public float duracionOleada = 50f;   // 300 / 50 = 6 oleadas

    [Header("Puntuación final = zombies + tiempo + oleadas (pesos ajustables)")]
    public int puntosPorZombie = 10;
    public int puntosPorSegundo = 1;
    public int puntosPorOleada = 50;

    [Header("Tienda de mejoras (0 = Potencia, 1 = Cadencia, 2 = Escudo)")]
    public int[] costosMejora = { 50, 100, 175, 275, 400, 550 };
    public float danioPorNivel = 0.5f;          // Potencia: +daño por disparo
    public float cadenciaPorNivel = 0.75f;      // Cadencia: +disparos por segundo
    public float cooldownMenosPorNivel = 0.6f;  // Escudo: -segundos de recarga
    public float escudoMasPorNivel = 0.1f;      // Escudo: +segundos de duración

    public float PV { get; private set; }
    public int Puntos { get; private set; }               // puntos gastables en la tienda
    public int ZombiesEliminados { get; private set; }
    public float TiempoTranscurrido { get; private set; }
    public EstadoPartida Estado { get; private set; } = EstadoPartida.Menu;
    public bool Pausado { get; private set; }

    // Puesto en el ranking de la partida que acaba de terminar (1 = récord)
    public int PuestoRanking { get; private set; }

    // "Activo" = jugando y sin pausa (zombies, spawner y disparo se detienen si es false)
    public bool JuegoActivo => Estado == EstadoPartida.Jugando && !Pausado;
    public bool Terminada => Estado == EstadoPartida.Victoria || Estado == EstadoPartida.Derrota;
    public float TiempoRestante => Mathf.Max(0f, duracionPartida - TiempoTranscurrido);
    public int OleadasTotales => Mathf.CeilToInt(duracionPartida / duracionOleada);
    public int Oleada => Mathf.Min(OleadasTotales, 1 + Mathf.FloorToInt(TiempoTranscurrido / duracionOleada));

    public int OleadasCompletadas
    {
        get
        {
            if (Estado == EstadoPartida.Victoria) return OleadasTotales;
            return Mathf.Min(OleadasTotales, Mathf.FloorToInt(TiempoTranscurrido / duracionOleada));
        }
    }

    public int PuntuacionFinal =>
        ZombiesEliminados * puntosPorZombie +
        Mathf.FloorToInt(TiempoTranscurrido) * puntosPorSegundo +
        OleadasCompletadas * puntosPorOleada;

    // Escudo
    float escudoHasta;
    float cooldownHasta;
    public bool EscudoActivo => Time.time < escudoHasta;
    public float CooldownRestante => Mathf.Max(0f, cooldownHasta - Time.time);
    public bool PuedeDefender => JuegoActivo && Time.time >= cooldownHasta;

    // Mejoras
    public int[] NivelMejora { get; } = new int[3];
    public int NivelMax => costosMejora.Length;
    public int CostoMejora(int i) => NivelMejora[i] < NivelMax ? costosMejora[NivelMejora[i]] : -1;
    public bool PuedeComprar(int i) => Estado == EstadoPartida.Jugando && CostoMejora(i) >= 0 && Puntos >= CostoMejora(i);

    // Aviso de golpe al jugador (lo usa el HUD). true = lo bloqueó el escudo
    public event System.Action<bool> AlGolpe;

    void Awake()
    {
        Instancia = this;
        PV = pvMax;
        Time.timeScale = 1f; // por si se reinició desde la pausa

        // La arena se agrega sola (solo trabaja en el estado Preparando)
        if (usarArena && GetComponent<Arena>() == null) gameObject.AddComponent<Arena>();

        // Primera vez: menú. Con "JUGAR DE NUEVO": directo a la partida.
        if (saltarMenu) Estado = usarArena ? EstadoPartida.Preparando : EstadoPartida.Jugando;
        else Estado = EstadoPartida.Menu;
        saltarMenu = false;
    }

    void Update()
    {
        if (!JuegoActivo) return;

        TiempoTranscurrido += Time.deltaTime;
        if (TiempoTranscurrido >= duracionPartida)
        {
            TiempoTranscurrido = duracionPartida;
            Terminar(EstadoPartida.Victoria);
        }
    }

    // ---------- Flujo: menú → arena → partida → fin ----------

    // Botón JUGAR del menú
    public void EmpezarPartida()
    {
        if (Estado != EstadoPartida.Menu) return;
        Estado = usarArena ? EstadoPartida.Preparando : EstadoPartida.Jugando;
    }

    // Lo llama la Arena al colocarla: arranca el cronómetro
    public void IniciarPartida()
    {
        if (Estado == EstadoPartida.Preparando) Estado = EstadoPartida.Jugando;
    }

    // Botones de la pantalla de fin
    public void JugarDeNuevo()
    {
        saltarMenu = true;
        Recargar();
    }

    public void IrAlMenu()
    {
        saltarMenu = false;
        Recargar();
    }

    void Recargar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Pausa (la usa la tienda): congela tiempo, zombies, escudo y cooldown
    public void Pausar(bool pausar)
    {
        if (Estado != EstadoPartida.Jugando) pausar = false;
        Pausado = pausar;
        Time.timeScale = pausar ? 0f : 1f;
    }

    // ---------- Combate ----------

    // Botón DEFENDER
    public void Defender()
    {
        if (!PuedeDefender) return;
        escudoHasta = Time.time + duracionEscudo;
        cooldownHasta = Time.time + cooldownEscudo;
    }

    public void RecibirDanio(float cantidad)
    {
        if (!JuegoActivo) return;
        if (EscudoActivo)   // el escudo bloquea el golpe
        {
            AlGolpe?.Invoke(true);
            return;
        }
        PV = Mathf.Max(0f, PV - cantidad);
        AlGolpe?.Invoke(false);
        if (PV <= 0f) Terminar(EstadoPartida.Derrota);
    }

    public void SumarPuntos(int cantidad)
    {
        if (!JuegoActivo) return;
        Puntos += cantidad;
        ZombiesEliminados++;
    }

    // Tienda: devuelve true si se pudo comprar
    public bool Comprar(int i)
    {
        if (i < 0 || i > 2 || !PuedeComprar(i)) return false;

        Puntos -= CostoMejora(i);
        NivelMejora[i]++;
        int n = NivelMejora[i];

        DisparoJugador d = FindAnyObjectByType<DisparoJugador>();
        switch (i)
        {
            case 0: // Potencia de disparo
                if (d != null) d.danio += danioPorNivel;
                break;
            case 1: // Cadencia / proyectiles (+1 bala en el nivel 3 y en el máximo)
                if (d != null)
                {
                    d.cadencia += cadenciaPorNivel;
                    d.proyectiles = 1 + (n >= 3 ? 1 : 0) + (n >= NivelMax ? 1 : 0);
                }
                break;
            case 2: // Escudo / enfriamiento
                cooldownEscudo = Mathf.Max(1f, cooldownEscudo - cooldownMenosPorNivel);
                duracionEscudo += escudoMasPorNivel;
                break;
        }
        return true;
    }

    void Terminar(EstadoPartida resultado)
    {
        if (Terminada) return;
        Estado = resultado;
        Pausado = false;
        Time.timeScale = 1f;

        // Guarda la partida en el ranking local
        string alias = Ranking.Alias;
        PuestoRanking = Ranking.Agregar(new Partida
        {
            alias = string.IsNullOrEmpty(alias) ? "JUGADOR" : alias,
            puntaje = PuntuacionFinal,
            zombies = ZombiesEliminados,
            segundos = Mathf.FloorToInt(TiempoTranscurrido),
            oleadas = OleadasCompletadas,
            oleadasTotales = OleadasTotales,
            victoria = resultado == EstadoPartida.Victoria,
            fecha = System.DateTime.Now.ToString("dd/MM/yyyy")
        });
    }
}
