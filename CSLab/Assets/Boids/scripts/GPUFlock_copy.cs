using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUFlock_copy: MonoBehaviour {

    public ComputeShader cshader;

    public GameObject boidPrefab;
    public int boidsCount;//要大于256
    public GameObject[] boidsGo;
    public GPUBoid[] boidsData;
    public float spawnRadius;
    public float flockSpeed;
    public float flockRotSpeed=4.0f;
    public float nearbyDis;

    private Vector3 targetPos = Vector3.zero;
    private int kernelHandle;
    private ComputeBuffer cbuffer;

    void Start()
    {
        
        kernelHandle = cshader.FindKernel("CSMain");

        //int stride = sizeof(GPUBoid);
        //int stride=sizeof(Vector3)*2+sizeof(float)*2;
        int stride=32;
        cbuffer = new ComputeBuffer(boidsCount, stride);

        boidsGo = new GameObject[boidsCount];
        boidsData = new GPUBoid[boidsCount];
        for (int i = 0; i < boidsCount; i++)
        {
            boidsData[i] = CreateBoidData();
            boidsGo[i] = Instantiate(boidPrefab, boidsData[i].pos, Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f)) as GameObject;
            boidsData[i].rot = boidsGo[i].transform.forward;
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

    void Update()
    {
        targetPos += new Vector3(2f, 5f, 3f);
        transform.localPosition += new Vector3(
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.x) * -0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.y) * 0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.z) * 0.2f)
        );

        cshader.SetVector("gFlockPos", new Vector4(transform.position.x,transform.position.y,transform.position.z,0f) );
        cshader.SetFloat("gDeltaTime", Time.deltaTime);
        //cshader.SetBuffer(kernelHandle, "gBoidBuffer", cbuffer);
        cshader.Dispatch(kernelHandle, boidsCount / 256, 1, 1);//当boidsCount小于256时就等于0了

        cbuffer.GetData(boidsData);
        for (int i = 0; i < boidsData.Length; i++)
        {

            boidsGo[i].transform.localPosition = boidsData[i].pos;

            if (!boidsData[i].rot.Equals(Vector3.zero))
            {
                boidsGo[i].transform.rotation = Quaternion.LookRotation(boidsData[i].rot);
            }
        }

    }

}
