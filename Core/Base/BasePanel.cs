using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{

    public void InitByArgs(Dictionary<string, object> args)
    {
        
    }

    public void CloseSelf()
    {
        Destroy(gameObject);
    }
    
}
