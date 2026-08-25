using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class cameraswap1 : MonoBehaviour
{
    public List<CinemachineVirtualCamera> aLLCameras = new List<CinemachineVirtualCamera>();
    public int camnumber = 0;
    // Start is called before the first frame update
    void Start()
    {
        ChangeCamera();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            camnumber = camnumber + 1;
            ChangeCamera();
        }
    }
    void ChangeCamera()
    {
        for (int i = 0; i < aLLCameras.Count; i++)
        {
            if (aLLCameras.Count -1 < camnumber)
            {
                camnumber = 0;
            }
            if (camnumber == i)
            {
                aLLCameras[i].gameObject.SetActive(true);
            }
            else
            {
                aLLCameras[i].gameObject.SetActive(false);
            }
        }
    }
}
