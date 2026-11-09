using UnityEngine;
using UnityEngine.EventSystems;

// Botón que se puede mantener presionado (lo usa ATACAR para disparar en ráfaga).
public class BotonMantener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool Presionado { get; private set; }

    public void OnPointerDown(PointerEventData e) { Presionado = true; }
    public void OnPointerUp(PointerEventData e) { Presionado = false; }

    void OnDisable() { Presionado = false; }
}
