using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUFlock_middle_plus : MonoBehaviour {
    public ComputeShader cshader;
    public Material material;
    public Mesh mesh;

    public int boidsCount;//要大于256
    public float spawnRadius;
    public float flockSpeed;
    public float flockRotSpeed=4.0f;
    public float nearbyDis;

    private Vector3 targetPos = Vector3.zero;
    private int kernelHandle;
    private ComputeBuffer cbuffer;

	void Start () 
    {
        kernelHandle = cshader.FindKernel("CSMain");

        int stride=32;
        cbuffer = new ComputeBuffer(boidsCount, stride);
        //just for initialization,cpu to gpu
        GPUBoid[] boidsData = new GPUBoid[boidsCount];
        for (int i = 0; i < boidsCount; i++)
        {
            boidsData[i] = CreateBoidData();
        }
        cbuffer.SetData(boidsData);

        cshader.SetBuffer(kernelHandle, "gBoidBuffer", cbuffer);
        cshader.SetInt("gBoidsCount",boidsCount);
        cshader.SetFloat("gNearByDist", nearbyDis);
	}
	
    GPUBoid CreateBoidData()
    {
        GPUBoid boidData = new GPUBoid();
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        boidData.pos = pos;
        boidData.rot = new Vector3(0f, 0f, 1f);
        boidData.speed = flockSpeed + Random.Range(-0.5f, 0.5f);
        boidData.rotSpeed=flockRotSpeed+Random.Range(-0.5f, 0.5f);

        return boidData;
    }

	void Update () 
    {
        targetPos += new Vector3(2f, 5f, 3f);
        transform.localPosition += new Vector3(
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.x) * -0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.y) * 0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.z) * 0.2f)
        );

        cshader.SetVector("gFlockPos", new Vector4(transform.position.x,transform.position.y,transform.position.z,0f) );
        cshader.SetFloat("gDeltaTime", Time.deltaTime);
        cshader.Dispatch(kernelHandle, boidsCount / 256, 1, 1);//当boidsCount小于256时就等于0了
	}

    void OnRenderObject()
    {

        for (int i = 0; i < boidsCount; i++)
        {
            material.SetPass(0);
            material.SetInt("_boidID",i);
            material.SetBuffer("_boidBuffer", cbuffer);
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
        }     
    }

}
