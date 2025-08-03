using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhoneController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TaskManager taskManager;

    public void OnPointerDown(PointerEventData eventData)
    {
        taskManager.SendRayHoldState(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        taskManager.SendRaySelectionRequest();
        Invoke(nameof(DelayedRayRelease), 0.05f);
    }

    private void DelayedRayRelease()
    {
        taskManager.SendRayHoldState(false);
    }
}
