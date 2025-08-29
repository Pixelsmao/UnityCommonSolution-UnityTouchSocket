using SRDebugger;
using UnityEngine;

public class Show_SROptons : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        SRDebug.Instance.ShowDebugPanel(DefaultTabs.Options);
    }

    // Update is called once per frame
    private void Update()
    {

    }
}
