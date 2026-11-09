using System.Collections.Generic;
using UnityEngine;

// Una partida guardada en el ranking
[System.Serializable]
public class Partida
{
    public string alias;
    public int puntaje;
    public int zombies;
    public int segundos;
    public int oleadas;
    public int oleadasTotales;
    public bool victoria;
    public string fecha;
}

// Ranking local: se guarda en el celular (PlayerPrefs en formato JSON). Sin nube.
public static class Ranking
{
    const string ClaveRanking = "zpark_ranking";
    const string ClaveAlias = "zpark_alias";
    const int MaxGuardadas = 50;

    [System.Serializable]
    class ListaPartidas { public List<Partida> partidas = new List<Partida>(); }

    public static string Alias
    {
        get { return PlayerPrefs.GetString(ClaveAlias, ""); }
        set { PlayerPrefs.SetString(ClaveAlias, value); PlayerPrefs.Save(); }
    }

    // Todas las partidas, ordenadas de mayor a menor puntaje
    public static List<Partida> Cargar()
    {
        string json = PlayerPrefs.GetString(ClaveRanking, "");
        if (string.IsNullOrEmpty(json)) return new List<Partida>();
        try
        {
            ListaPartidas l = JsonUtility.FromJson<ListaPartidas>(json);
            return l != null && l.partidas != null ? l.partidas : new List<Partida>();
        }
        catch
        {
            return new List<Partida>();
        }
    }

    // Guarda la partida y devuelve su puesto (1 = récord). 0 si quedó fuera de la lista.
    public static int Agregar(Partida p)
    {
        List<Partida> lista = Cargar();
        lista.Add(p);
        lista.Sort((a, b) => b.puntaje.CompareTo(a.puntaje));
        if (lista.Count > MaxGuardadas) lista.RemoveRange(MaxGuardadas, lista.Count - MaxGuardadas);

        PlayerPrefs.SetString(ClaveRanking, JsonUtility.ToJson(new ListaPartidas { partidas = lista }));
        PlayerPrefs.Save();
        return lista.IndexOf(p) + 1;
    }

    public static Partida Mejor()
    {
        List<Partida> l = Cargar();
        return l.Count > 0 ? l[0] : null;
    }
}
