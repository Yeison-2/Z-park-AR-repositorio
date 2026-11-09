using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Arena / campo de batalla sobre el suelo detectado.
// El GameManager la agrega solo. Ella agrega AR Plane Manager y AR Raycast Manager al XR Origin.
public class Arena : MonoBehaviour
{
    public static Arena Instancia { get; private set; }

    [Tooltip("Radio de la arena en metros (7.5 m = unos 15 m de diámetro)")]
    public float radio = 7.5f;

    public bool Colocada { get; private set; }
    public Vector3 Centro { get; private set; }
    public float AlturaSuelo { get; private set; }

    // Para el HUD: ¿ya vio algún suelo? ¿la mira está sobre el suelo ahora mismo?
    public bool HaySuelo
    {
        get
        {
            try { return planos != null && planos.trackables.count > 0; }
            catch { return false; }
        }
    }
    public bool ApuntaAlSuelo { get; private set; }

    // Primer error de AR atrapado (se muestra en la pantalla de escaneo para poder reportarlo)
    public string UltimoError { get; private set; }

    [Tooltip("Segundos sin detectar suelo antes de permitir jugar con la altura estimada")]
    public float esperaRespaldo = 12f;

    // Si tarda demasiado en encontrar el suelo, se puede jugar con altura estimada
    public bool RespaldoDisponible => !Colocada && !ApuntaAlSuelo && Time.unscaledTime - inicio > esperaRespaldo;

    static readonly Color Verde = new Color(0.65f, 0.89f, 0.16f);
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    ARRaycastManager raycaster;
    ARPlaneManager planos;
    Transform cam;

    LineRenderer reticula;      // anillo pequeño donde apunta la mira
    LineRenderer borde;         // anillo grande de la arena
    LineRenderer marcaCentro;   // anillo pequeño fijo en el centro
    Vector3 ultimoPunto;
    float inicio;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        cam = Camera.main.transform;
        inicio = Time.unscaledTime;

        XROrigin origen = FindAnyObjectByType<XROrigin>();
        if (origen != null)
        {
            raycaster = Obtener<ARRaycastManager>(origen.gameObject);
            planos = Obtener<ARPlaneManager>(origen.gameObject);
            planos.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        reticula = CrearAnillo("Reticula", 0.35f, 0.03f, Verde);
        borde = CrearAnillo("Borde Arena", radio, 0.08f, new Color(Verde.r, Verde.g, Verde.b, 0.35f));
        marcaCentro = CrearAnillo("Centro Arena", 0.3f, 0.04f, Verde);
        reticula.gameObject.SetActive(false);
        borde.gameObject.SetActive(false);
        marcaCentro.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Colocada) return;

        // Solo trabaja después de tocar JUGAR (estado Preparando)
        GameManager gm = GameManager.Instancia;
        if (gm != null && gm.Estado != EstadoPartida.Preparando)
        {
            inicio = Time.unscaledTime;
            ApuntaAlSuelo = false;
            reticula.gameObject.SetActive(false);
            borde.gameObject.SetActive(false);
            return;
        }

#if UNITY_EDITOR
        // En el editor no hay AR: coloca la arena bajo la cámara y empieza
        Colocar(new Vector3(cam.position.x, cam.position.y - 1.4f, cam.position.z));
        return;
#endif

        // Busca el suelo justo donde apunta la mira (centro de la pantalla)
        // Protegido: si el AR de algún celular falla, el juego sigue y la opción de respaldo funciona
        ApuntaAlSuelo = false;
        try
        {
            hits.Clear();
            if (raycaster != null && raycaster.enabled &&
                raycaster.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), hits,
                                  TrackableType.PlaneWithinPolygon) &&
                hits.Count > 0)
            {
                ApuntaAlSuelo = true;
                ultimoPunto = hits[0].pose.position;
            }
        }
        catch (System.Exception e)
        {
            ApuntaAlSuelo = false;
            if (UltimoError == null)
            {
                UltimoError = e.GetType().Name + ": " + e.Message;
                Debug.LogWarning("ZPark Arena: " + e);
            }
        }

        reticula.gameObject.SetActive(ApuntaAlSuelo);
        borde.gameObject.SetActive(ApuntaAlSuelo);
        if (ApuntaAlSuelo)
        {
            Vector3 p = ultimoPunto + Vector3.up * 0.01f;
            reticula.transform.position = p;
            reticula.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f));
            borde.transform.position = p; // vista previa del tamaño de la arena
        }

        // Toque para colocar (se ignora el primer medio segundo)
        if (Time.unscaledTime - inicio > 0.5f && Toco())
        {
            if (ApuntaAlSuelo) Colocar(ultimoPunto);
            else if (RespaldoDisponible) Colocar(PuntoEstimado());
        }
    }

    // Suelo estimado: 2 m delante del jugador, 1.4 m bajo la cámara
    Vector3 PuntoEstimado()
    {
        Vector3 adelante = cam.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 0.01f) adelante = Vector3.forward;
        Vector3 p = cam.position + adelante.normalized * 2f;
        p.y = cam.position.y - 1.4f;
        return p;
    }

    void Colocar(Vector3 punto)
    {
        Colocada = true;
        Centro = punto;
        AlturaSuelo = punto.y;

        reticula.gameObject.SetActive(false);

        Vector3 p = punto + Vector3.up * 0.01f;
        borde.transform.position = p;
        borde.startColor = borde.endColor = new Color(Verde.r, Verde.g, Verde.b, 0.9f);
        borde.gameObject.SetActive(true);
        marcaCentro.transform.position = p;
        marcaCentro.gameObject.SetActive(true);

        // Ya no hace falta buscar más suelo: ARCore trabaja menos (más estable y menos batería)
        if (planos != null) planos.enabled = false;

        if (GameManager.Instancia != null) GameManager.Instancia.IniciarPartida();
    }

    static bool Toco()
    {
        bool toque = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool clic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return toque || clic;
    }

    static T Obtener<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static LineRenderer CrearAnillo(string nombre, float r, float grosor, Color color)
    {
        GameObject go = new GameObject(nombre);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = grosor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null) lr.material = new Material(sh);
        lr.startColor = lr.endColor = color;

        const int n = 96;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
        return lr;
    }
}
