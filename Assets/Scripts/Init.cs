using TouchSocket.Core;
using UnityEngine;

public class Init : MonoBehaviour
{
    private void Awake()
    {
        GlobalEnvironment.DynamicBuilderType = DynamicBuilderType.Expression;
        WebGLTimeoutExtension.Init(this);
    }

    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
    }
}