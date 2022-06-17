using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeformationToucher : MonoBehaviour
{
    public MeshFilter targetMeshFilter;
    private Mesh targetMesh;

    public Camera mainCamera;

    private Vector3[] originalVertices, displacedVertices, vertexVelocities;

    private int verticesCount;

    public float force = 10;
    public float forceOffset = 0.1f;
    public float springForce = 20f;
    public float damping = 5f;

    Vector3 fingerPosition;

    // Start is called before the first frame update
    void Start()
    {
        targetMesh = targetMeshFilter.mesh;

        verticesCount = targetMesh.vertices.Length;

        originalVertices = targetMesh.vertices;
        displacedVertices = targetMesh.vertices;
        vertexVelocities = new Vector3[verticesCount];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (true)
        //if (Input.GetMouseButton(0))
        {
            fingerPosition = new Vector3(1, 1, 0);///////////////////////////////////////////////////////////////////改改改/
            if (Physics.Raycast(mainCamera.ScreenPointToRay(fingerPosition), out RaycastHit hitInfo))
            //if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
            {
                Vector3 actingForcePoint = targetMeshFilter.transform.InverseTransformPoint(hitInfo.point + hitInfo.normal * forceOffset);//发力点指向球的本地坐标向量

                for (int i = 0; i < verticesCount; i++)
                {
                    Vector3 pointToVertex = displacedVertices[i] - actingForcePoint;//作用力点指向当前顶点位置的向量

                    float actingForce = force / (1f + pointToVertex.sqrMagnitude);//作用力大小
                    vertexVelocities[i] += pointToVertex.normalized * actingForce * Time.deltaTime;//顶点速度向量
                }
            }
        }

        for (int i = 0; i < verticesCount; i++)
        {
            vertexVelocities[i] += (originalVertices[i] - displacedVertices[i]) * springForce * Time.deltaTime;//加上+顶点当前位置指向顶点初始位置的速度向量==回弹力
            vertexVelocities[i] *= 1f - damping * Time.deltaTime;//乘上阻力
            displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;//算出顶点的下一个位置
        }

        targetMesh.vertices = displacedVertices;
        targetMesh.RecalculateNormals();
    }
}
