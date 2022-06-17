using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeformMeshInput : MonoBehaviour
{
    public float force = 10f;
    public float forceOffset = 0.1f;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(inputRay, out hit))
        {
            DeformMesh deformer = hit.collider.GetComponent<DeformMesh>();

            if (deformer)
            {
                Vector3 point = hit.point;
                point += hit.normal * forceOffset;
                deformer.AddDeformingForce(point, Vector3.down * 0.02f, 1);
            }
        }

        
    }
}
