using UnityEngine;
using UnityEngine.EventSystems; // Importante para as interfaces de evento

public class UIPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Flag que indica se o botão está sendo pressionado
    public bool IsBeingPressed { get; private set; } = false;

    // Método chamado quando o ponteiro (mouse/toque) é pressionado sobre o objeto
    public void OnPointerDown(PointerEventData eventData)
    {
        IsBeingPressed = true;
        // Debug.Log($"Botão {gameObject.name} Pressionado!"); // Opcional para depuração
    }

    // Método chamado quando o ponteiro (mouse/toque) é solto sobre o objeto
    public void OnPointerUp(PointerEventData eventData)
    {
        IsBeingPressed = false;
        // Debug.Log($"Botão {gameObject.name} Solto!"); // Opcional para depuração
    }

    // Opcional: Para garantir que a flag seja resetada se o objeto for desativado
    void OnDisable()
    {
        IsBeingPressed = false;
    }
}