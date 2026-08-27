using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x namespace (was "Cinemachine" in 2.x)
public class cameraswap1 : MonoBehaviour
{
    public List<CinemachineCamera> aLLCameras = new List<CinemachineCamera>(); // was CinemachineVirtualCamera in 2.x
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
            if (aLLCameras.Count - 1 < camnumber)
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

    // lets other scripts (like PlayerCarInteraction) jump straight to a
    // specific camera by index, instead of only being able to cycle with V.
    public void SwitchTo(int index)
    {
        camnumber = index;
        ChangeCamera();
    }
}