using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRRestartHelper : MonoBehaviour
{
    public void RestartXR()
    {
        StartCoroutine(RestartXRCoroutine());
    }

    private IEnumerator RestartXRCoroutine()
    {
        // Parar e desalocar o XR
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();

        // Aguarde um frame
        yield return null;

        // Inicializa novamente
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Falha ao inicializar o XR Loader");
            yield break;
        }

        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }
}
