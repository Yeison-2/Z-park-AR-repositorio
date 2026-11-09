using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public ZombieData[] tipos;

    [Tooltip("Distancia mínima al jugador para aparecer")]
    public float distanciaMin = 4f;
    [Tooltip("Solo se usa si no hay arena colocada")]
    public float distanciaMax = 7f;

    [Header("Base (oleada 1). Cada oleada sube la dificultad")]
    public float intervalo = 3f;
    public int maxZombies = 8;
    [Range(0.5f, 1f)] public float reduccionIntervaloPorOleada = 0.85f;
    public int zombiesExtraPorOleada = 2;
    public float intervaloMinimo = 0.8f;

    float temporizador;

    int OleadaActual => GameManager.Instancia != null ? GameManager.Instancia.Oleada : 1;

    float IntervaloActual =>
        Mathf.Max(intervaloMinimo, intervalo * Mathf.Pow(reduccionIntervaloPorOleada, OleadaActual - 1));

    int MaxActual => maxZombies + (OleadaActual - 1) * zombiesExtraPorOleada;

    void Update()
    {
        if (GameManager.Instancia != null && !GameManager.Instancia.JuegoActivo) return;

        temporizador += Time.deltaTime;
        if (temporizador < IntervaloActual) return;
        temporizador = 0f;

        if (Zombie.Activos >= MaxActual) return;
        Aparecer();
    }

    void Aparecer()
    {
        Transform cam = Camera.main.transform;
        Vector3 pos = PuntoAparicion(cam);

        // Cada tipo usa su modelo 3D; si no tiene, la cápsula de siempre
        ZombieData tipo = ElegirTipo();
        bool conModelo = tipo != null && tipo.modelo != null;
        GameObject prefab = conModelo ? tipo.modelo : zombiePrefab;
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        Zombie z = go.GetComponent<Zombie>();
        if (z != null) z.Configurar(tipo);

        // La cápsula tiene el centro en el medio: se sube para apoyarla en el suelo.
        // El modelo 3D ya trae el pivote en los pies.
        if (!conModelo)
        {
            Renderer r = go.GetComponentInChildren<Renderer>();
            if (r != null) go.transform.position += Vector3.up * (pos.y - r.bounds.min.y);
        }
    }

    // Punto en el suelo ALREDEDOR DEL JUGADOR (entre Distancia Min y Distancia Max),
    // dentro de la arena si está colocada y a la altura real del suelo
    Vector3 PuntoAparicion(Transform cam)
    {
        Arena a = Arena.Instancia;
        bool hayArena = a != null && a.Colocada;
        float suelo = hayArena ? a.AlturaSuelo : cam.position.y - 1.4f;
        Vector3 jugador = new Vector3(cam.position.x, suelo, cam.position.z);

        Vector3 p = jugador;
        for (int i = 0; i < 12; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(distanciaMin, distanciaMax);
            p = jugador + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);

            // Con arena, se prefiere un punto dentro del círculo
            if (!hayArena || Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(a.Centro.x, 0f, a.Centro.z)) <= a.radio)
                return p;
        }
        return p; // el jugador está fuera de la arena: aparece igual cerca de él
    }

    // Elige un tipo al azar según su "peso", solo entre los ya desbloqueados en esta oleada
    ZombieData ElegirTipo()
    {
        if (tipos == null || tipos.Length == 0) return null;

        int ola = OleadaActual;
        float total = 0f;
        foreach (ZombieData t in tipos)
            if (t != null && t.oleadaMinima <= ola) total += t.peso;
        if (total <= 0f) return null;

        float azar = Random.Range(0f, total);
        foreach (ZombieData t in tipos)
        {
            if (t == null || t.oleadaMinima > ola) continue;
            azar -= t.peso;
            if (azar <= 0f) return t;
        }
        return tipos[0];
    }
}
