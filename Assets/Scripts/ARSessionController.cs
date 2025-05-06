using UnityEngine;
using UnityEngine.XR.Management;
using System.Collections;

public class ARSessionController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private XRRestartHelper xrHelper;

    void Awake()
    {
        xrHelper = GetComponent<XRRestartHelper>();
    }

    void OnDisable()
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
    }

    void OnEnable()
    {
        if (xrHelper != null)
        {
            xrHelper.RestartXR();
        }
    }
    public void StartAR()
    {
        StartCoroutine(StartXR());
    }

    public void StopAR()
    {
        StopCoroutine(StartXR());
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
    }

    public IEnumerator StartXR()
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }
}
