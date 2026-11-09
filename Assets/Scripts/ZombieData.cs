using UnityEngine;

// Datos de un tipo de zombie. Se edita desde el Inspector, sin tocar código.
[CreateAssetMenu(menuName = "ZPark/Zombie Data", fileName = "NuevoZombie")]
public class ZombieData : ScriptableObject
{
    public string nombre = "Infectado";
    public float vida = 3f;
    public float velocidad = 0.8f;
    public float danio = 10f;
    public int puntos = 10;

    [Tooltip("Multiplicador de tamaño sobre el prefab")]
    public float escala = 1f;

    [Tooltip("Color de la cápsula (no se usa si hay modelo 3D, que trae su propia textura)")]
    public Color color = new Color(0.55f, 0.75f, 0.2f);

    [Tooltip("Probabilidad relativa de aparición (más alto = aparece más)")]
    [Range(0f, 10f)] public float peso = 1f;

    [Tooltip("Primera oleada en la que puede aparecer")]
    public int oleadaMinima = 1;

    [Tooltip("Prefab con el modelo 3D. Vacío = usa la cápsula del spawner. Lo llena el menú ZPark > Configurar zombies 3D")]
    public GameObject modelo;
}
