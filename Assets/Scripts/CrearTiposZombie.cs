#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú ZPark > Crear tipos de zombie
// Crea (o actualiza) los 3 ZombieData en Assets/Data y los asigna al ZombieSpawner de la escena.
public static class CrearTiposZombie
{
    [MenuItem("ZPark/Crear tipos de zombie")]
    static void Crear()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        //                               nombre       vida  vel   daño  pts  esc    color                              peso  oleada
        ZombieData basico    = Hacer("Infectado",   3f, 0.8f, 10f, 10, 1.00f, new Color(0.55f, 0.75f, 0.20f), 5f, 1);
        ZombieData corredor  = Hacer("Corredor",    2f, 2.0f,  8f, 20, 0.85f, new Color(0.90f, 0.45f, 0.15f), 3f, 2);
        ZombieData acechador = Hacer("Acechador",   6f, 0.5f, 25f, 40, 1.25f, new Color(0.45f, 0.20f, 0.55f), 2f, 3);

        AssetDatabase.SaveAssets();

        ZombieSpawner spawner = Object.FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Asignar tipos de zombie");
            spawner.tipos = new[] { basico, corredor, acechador };
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            Debug.Log("ZPark: 3 tipos de zombie creados y asignados al ZombieSpawner.");
        }
        else
        {
            Debug.LogWarning("ZPark: tipos creados en Assets/Data, pero no hay ZombieSpawner en la escena. Arrástralos al campo Tipos.");
        }
    }

    static ZombieData Hacer(string nombre, float vida, float vel, float danio, int puntos,
                            float escala, Color color, float peso, int oleadaMinima)
    {
        string ruta = "Assets/Data/" + nombre + ".asset";
        ZombieData d = AssetDatabase.LoadAssetAtPath<ZombieData>(ruta);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<ZombieData>();
            AssetDatabase.CreateAsset(d, ruta);
        }

        d.nombre = nombre;
        d.vida = vida;
        d.velocidad = vel;
        d.danio = danio;
        d.puntos = puntos;
        d.escala = escala;
        d.color = color;
        d.peso = peso;
        d.oleadaMinima = oleadaMinima;
        EditorUtility.SetDirty(d);
        return d;
    }
}
#endif
