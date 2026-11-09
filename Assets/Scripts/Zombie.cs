using System.Collections.Generic;
using UnityEngine;

public class Zombie : MonoBehaviour
{
    // Contador de zombies vivos (lo usa el spawner)
    public static int Activos { get; private set; }

    // Aviso al morir (lo usa el HUD para el "+puntos" y el sonido): posición, puntos, color
    public static event System.Action<Vector3, int, Color> AlMorir;

    [Header("Atributos (los pisa ZombieData al aparecer)")]
    public float vida = 3f;
    public float velocidad = 0.8f;
    public float danio = 10f;
    public int puntos = 10;
    public float distanciaAtaque = 0.9f;

    [Header("Animación (solo si tiene modelo 3D)")]
    [Tooltip("Velocidad a la que la animación Caminar se ve natural (m/s)")]
    public float velocidadAnimCaminar = 0.8f;
    [Tooltip("Velocidad a la que la animación Correr se ve natural (m/s)")]
    public float velocidadAnimCorrer = 2f;
    [Tooltip("Segundo del golpe dentro de Atacar (ahí se hace el daño)")]
    public float momentoGolpe = 0.4f;
    public float duracionAtaque = 0.75f;
    public float duracionMuerte = 1.6f;

    Transform objetivo;
    Color colorTipo = new Color(0.55f, 0.75f, 0.2f);

    // Animaciones
    Animator anim;
    string animMover = "Caminar";
    string animActual;
    bool atacando, golpeDado, muerto;
    float tAtaque;

    // Destello rojo al recibir un disparo
    readonly List<Material> materiales = new List<Material>();
    readonly List<Color> coloresBase = new List<Color>();
    Vector3 escalaBase;
    float golpeT;

    static Material matParticulas;

    // Zombies en escena (lo usa el HUD para las flechas de los que están fuera de la vista)
    public static readonly List<Zombie> Todos = new List<Zombie>();
    public bool Muerto => muerto;

    void OnEnable()  { Activos++; Todos.Add(this); }
    void OnDisable() { Activos--; Todos.Remove(this); }

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    // El spawner llama a esto justo después de crear el zombie
    public void Configurar(ZombieData datos)
    {
        if (datos == null) return;

        vida = datos.vida;
        velocidad = datos.velocidad;
        danio = datos.danio;
        puntos = datos.puntos;
        colorTipo = datos.color;

        transform.localScale *= datos.escala;

        // Solo la cápsula se pinta: el modelo 3D trae su textura
        if (datos.modelo == null)
        {
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = datos.color;
        }

        // El Corredor usa "Correr" si su modelo la tiene
        if (TieneAnim("Correr") && velocidad >= 1.5f) animMover = "Correr";
    }

    void Start()
    {
        objetivo = Camera.main.transform;
        escalaBase = transform.localScale;

        // Guarda los colores originales de todos los materiales (cápsula o modelo real)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)
            {
                if (!m.HasProperty("_BaseColor") && !m.HasProperty("_Color")) continue;
                materiales.Add(m);
                coloresBase.Add(m.color);
            }
        }

        // Arranca la animación de caminar desde ya (cada zombie en un punto distinto del paso)
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.enabled = true;
            anim.Rebind();
            if (TieneAnim(animMover))
            {
                anim.Play(animMover, 0, Random.value);
                animActual = animMover;
                anim.speed = animMover == "Correr"
                    ? Mathf.Clamp(velocidad / velocidadAnimCorrer, 0.6f, 1.5f)
                    : Mathf.Clamp(velocidad / velocidadAnimCaminar, 0.6f, 1.5f);
            }
        }
        Reproducir(animMover);
    }

    void Update()
    {
        // Destello al recibir daño (se apaga solo)
        if (golpeT > 0f)
        {
            golpeT = Mathf.Max(0f, golpeT - Time.deltaTime * 6f);
            Color golpe = new Color(1f, 0.15f, 0.1f);
            for (int i = 0; i < materiales.Count; i++)
                materiales[i].color = Color.Lerp(coloresBase[i], golpe, golpeT);
            transform.localScale = escalaBase * (1f + 0.1f * golpeT);
        }

        if (muerto || objetivo == null) return;

        GameManager gm = GameManager.Instancia;
        if (gm != null && !gm.JuegoActivo)
        {
            // Partida terminada: se quedan quietos (en pausa el tiempo ya está congelado)
            if (!gm.Pausado) Reproducir("Quieto");
            return;
        }

        if (atacando)
        {
            Atacando();
            return;
        }

        // Camina hacia el jugador manteniendo su altura
        Vector3 destino = new Vector3(objetivo.position.x, transform.position.y, objetivo.position.z);
        float distancia = Vector3.Distance(transform.position, destino);

        transform.LookAt(destino);

        if (distancia > distanciaAtaque)
        {
            transform.position += DireccionConSeparacion(destino) * velocidad * Time.deltaTime;
            Reproducir(animMover);
        }
        else if (anim != null && TieneAnim("Atacar"))
        {
            // Modelo 3D: anima el golpe y hace el daño en el momento del impacto
            atacando = true;
            tAtaque = 0f;
            Reproducir("Atacar");
        }
        else
        {
            // Cápsula: golpe inmediato
            if (gm != null) gm.RecibirDanio(danio);
            Destroy(gameObject);
        }
    }

    void Atacando()
    {
        tAtaque += Time.deltaTime;
        if (!golpeDado && tAtaque >= momentoGolpe)
        {
            golpeDado = true;
            if (GameManager.Instancia != null) GameManager.Instancia.RecibirDanio(danio);
        }
        if (tAtaque >= duracionAtaque) Destroy(gameObject);
    }

    // Hacia el jugador, pero apartándose de los zombies que tenga muy cerca (así no se amontonan)
    const float RadioSeparacion = 0.9f;

    Vector3 DireccionConSeparacion(Vector3 destino)
    {
        Vector3 hacia = destino - transform.position;
        hacia.y = 0f;
        hacia = hacia.sqrMagnitude > 0.0001f ? hacia.normalized : Vector3.zero;

        Vector3 separar = Vector3.zero;
        foreach (Zombie o in Todos)
        {
            if (o == null || o == this || o.muerto) continue;
            Vector3 d = transform.position - o.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            if (m >= RadioSeparacion) continue;
            if (m < 0.01f) d = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)); // justo encima: lado al azar
            separar += d.normalized * (1f - m / RadioSeparacion);
        }

        Vector3 dir = hacia + separar * 1.5f;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : hacia;
    }

    public void RecibirDanio(float cantidad)
    {
        if (muerto || vida <= 0f) return;
        vida -= cantidad;
        golpeT = 1f;
        if (vida <= 0f) Morir();
    }

    void Morir()
    {
        muerto = true;

        Vector3 centro = transform.position;
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null) centro = r.bounds.center;

        Estallido(centro, colorTipo);
        AlMorir?.Invoke(centro, puntos, colorTipo);
        if (GameManager.Instancia != null) GameManager.Instancia.SumarPuntos(puntos);

        if (anim != null && TieneAnim("Morir"))
        {
            // Ya no recibe disparos: cae al suelo y desaparece al terminar la animación
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Reproducir("Morir");
            Destroy(gameObject, duracionMuerte);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------- Animación ----------

    bool TieneAnim(string nombre)
    {
        return anim != null && anim.runtimeAnimatorController != null &&
               anim.HasState(0, Animator.StringToHash(nombre));
    }

    void Reproducir(string nombre)
    {
        if (anim == null || animActual == nombre || !TieneAnim(nombre)) return;
        animActual = nombre;

        // Ajusta el paso a la velocidad real para que no "patine"
        if (nombre == "Caminar") anim.speed = Mathf.Clamp(velocidad / velocidadAnimCaminar, 0.6f, 1.5f);
        else if (nombre == "Correr") anim.speed = Mathf.Clamp(velocidad / velocidadAnimCorrer, 0.6f, 1.5f);
        else anim.speed = 1f;

        anim.CrossFadeInFixedTime(nombre, 0.15f, 0);
    }

    // Explosión de partículas del color del zombie (creada por código)
    static void Estallido(Vector3 pos, Color c)
    {
        GameObject go = new GameObject("Estallido");
        go.transform.position = pos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.black, 0.5f));
        main.gravityModifier = 1.2f;
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.2f;

        if (matParticulas == null)
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s != null) matParticulas = new Material(s);
        }
        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        if (matParticulas != null) pr.material = matParticulas;

        ps.Play();
    }
}
