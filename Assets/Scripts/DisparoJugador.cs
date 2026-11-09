using UnityEngine;

public class DisparoJugador : MonoBehaviour
{
    public float danio = 1f;
    public float alcance = 30f;

    [Tooltip("Disparos por segundo al mantener ATACAR (la tienda lo mejora)")]
    [Min(0.1f)] public float cadencia = 2f;

    [Tooltip("Balas por disparo (la tienda lo sube en los niveles 3 y 6 de Cadencia)")]
    [Min(1)] public int proyectiles = 1;

    [Tooltip("Grados de separación entre balas cuando hay más de una")]
    public float separacion = 2.5f;

    // Lo pone el HUD mientras el botón ATACAR está presionado
    [HideInInspector] public bool atacando;

    // Aviso en cada disparo (lo usa el HUD para sonido y mira). true = le dio a un zombie
    public event System.Action<bool> AlDisparar;

    float proximoDisparo;

    void Update()
    {
        if (GameManager.Instancia != null && !GameManager.Instancia.JuegoActivo) return;

        if (atacando && Time.time >= proximoDisparo)
        {
            proximoDisparo = Time.time + 1f / cadencia;
            Disparar();
        }
    }

    public void Disparar()
    {
        Transform cam = Camera.main.transform;
        bool acerto = false;

        for (int k = 0; k < proyectiles; k++)
        {
            // Las balas extra salen un poco abiertas hacia los lados
            float angulo = (k - (proyectiles - 1) * 0.5f) * separacion;
            Vector3 dir = Quaternion.AngleAxis(angulo, cam.up) * cam.forward;

            if (Physics.Raycast(cam.position, dir, out RaycastHit hit, alcance))
            {
                Zombie z = hit.collider.GetComponentInParent<Zombie>();
                if (z != null)
                {
                    z.RecibirDanio(danio);
                    acerto = true;
                }
            }
        }

        AlDisparar?.Invoke(acerto);
    }
}
