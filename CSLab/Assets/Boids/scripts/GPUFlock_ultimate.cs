using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GPUBoid
{
    public Vector3 pos, rot;
    public float speed, rotSpeed;
}
    

//replace boid prefab(game object) with just mesh
//gird optimization!
public class GPUFlock_ultimate: MonoBehaviour {
    readonly static int SIMULATION_BLOCK_SIZE=256;

    public ComputeShader boidCS;
    public ComputeShader sortCS;
    public Material material;
    public Mesh mesh;

    public int boidsCount;//要大于256
    public float spawnRadius;
    public float flockSpeed;
    public float flockRotSpeed=4.0f;
    public float nearbyDis;

    //debug
    //public GameObject debugCube;
    /*
   struct int2
    {
        public int x;
        public int y;
    }
    private int2[] sortedGridData = new int2[8192];
    private int2[] gridIndicesData=new int2[1<<24];
    */

    private Vector3 targetPos = Vector3.zero;

    private int calCenterKernelHandle;
    private int buildGridKernelHandle;
    private int clearGridIndicesKernelHandle;
    private int buildGridIndicesKernelHandle;
    private int rearrangeParticlesKernelHandle;
    private int boidsGridKernelHandle;

    //private ComputeBuffer cbBoid;
    //private ComputeBuffer cbBoidPre;
    //交替使用，不固定的——》有了simulation前的sort，其实是固定的，那就约定0为更新后，1为更新前但排序后吧
    private ComputeBuffer[] cbBoid=new ComputeBuffer[2];
    private ComputeBuffer cbGrid;
    private ComputeBuffer cbGridIndices;
    private ComputeBuffer cbBoidsCenter;

    private BitonicSorter2 sorter;

    void Start()
    {
        InitBoidCS();
        sorter = new BitonicSorter2(cbGrid, sortCS);
        sorter.SetGPUData(cbGrid);
    }

    void InitBoidCS()
    {
        calCenterKernelHandle = boidCS.FindKernel("CalCenterCS");
        buildGridKernelHandle = boidCS.FindKernel("BuildGridCS");
        clearGridIndicesKernelHandle = boidCS.FindKernel("ClearGridIndicesCS");
        buildGridIndicesKernelHandle = boidCS.FindKernel("BuildGridIndicesCS");
        rearrangeParticlesKernelHandle = boidCS.FindKernel("RearrangeParticlesCS");
        boidsGridKernelHandle = boidCS.FindKernel("BoidsCS_Grid");

        cbBoid[0] = new ComputeBuffer(boidsCount, 32);
        cbBoid [1]= new ComputeBuffer(boidsCount, 32);
        cbGrid = new ComputeBuffer(boidsCount, 8);
        //cbGridIndices = new ComputeBuffer(1<<24, 4);//xyz分别分配8位
        //start&end int2啊！艹！
        cbGridIndices = new ComputeBuffer(1<<24, 8);
        cbBoidsCenter = new ComputeBuffer(1, 12);

        //just for initialization,cpu to gpu
        GPUBoid[] boidsData = new GPUBoid[boidsCount];
        for (int i = 0; i < boidsCount; i++)
        {
            boidsData[i] = CreateBoidData();
        }
        cbBoid[0].SetData(boidsData);
        boidCS.SetInt("gBoidsCount",boidsCount);
        boidCS.SetFloat("gNearByDist", nearbyDis);
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
        UpdateLeader();
        Simulate();   
    }

    void UpdateLeader()
    {
        targetPos += new Vector3(2f, 5f, 3f);
        transform.localPosition += new Vector3(
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.x) * -0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.y) * 0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * targetPos.z) * 0.2f)
        );
    }

    void Simulate()
    {
        //calulate the current center before update
        boidCS.SetBuffer(calCenterKernelHandle,"BoidRO",cbBoid[0]);
        boidCS.SetBuffer(calCenterKernelHandle, "BoidsCenterRW", cbBoidsCenter);
        boidCS.Dispatch(calCenterKernelHandle, 1, 1, 1);
        /*
        //debug
        Vector3[] centerData=new Vector3[1];
        cbBoidsCenter.GetData(centerData);
        debugCube.transform.position = centerData[0];
        *///没问题
        //build grid
        boidCS.SetBuffer(buildGridKernelHandle,"GridRW",cbGrid);
        boidCS.SetBuffer(buildGridKernelHandle,"BoidRO",cbBoid[0]);
        //忘了这个！调用的函数用到了它
        boidCS.SetBuffer(buildGridKernelHandle,"BoidsCenterRW",cbBoidsCenter);
        boidCS.Dispatch(buildGridKernelHandle, boidsCount / SIMULATION_BLOCK_SIZE, 1, 1);
        //gpu sort
        sorter.GPUSort();
        /*
        //debug
        //cbGrid.GetData(sortedGridData);
        //问题出在这,cell号竟然有负数，出界了
        */
        //clear grid indices
        boidCS.SetBuffer(clearGridIndicesKernelHandle,"GridIndicesRW",cbGridIndices);
        boidCS.Dispatch(clearGridIndicesKernelHandle, cbGridIndices.count / SIMULATION_BLOCK_SIZE, 1, 1);
        //build grid indices
        boidCS.SetBuffer(buildGridIndicesKernelHandle,"GridIndicesRW",cbGridIndices);
        boidCS.SetBuffer(buildGridIndicesKernelHandle,"GridRO",cbGrid);
        boidCS.Dispatch(buildGridIndicesKernelHandle, boidsCount / SIMULATION_BLOCK_SIZE, 1, 1);
        /*
        //debug
        cbGrid.GetData(sortedGridData);
        cbGridIndices.GetData(gridIndicesData);
        //问题出在这，全是0——》stride错了。。。
        */
        //rearrange entity elements
        boidCS.SetBuffer(rearrangeParticlesKernelHandle,"BoidRO",cbBoid[0]);
        boidCS.SetBuffer(rearrangeParticlesKernelHandle,"BoidRW",cbBoid[1]);
        boidCS.SetBuffer(rearrangeParticlesKernelHandle,"GridRO",cbGrid);
        boidCS.Dispatch(rearrangeParticlesKernelHandle, boidsCount / SIMULATION_BLOCK_SIZE, 1, 1);
        //simulate!
        boidCS.SetVector("gFlockPos", new Vector4(transform.position.x,transform.position.y,transform.position.z,0f) );
        boidCS.SetFloat("gDeltaTime", Time.deltaTime);
        boidCS.SetBuffer(boidsGridKernelHandle, "BoidRO", cbBoid[1]);
        boidCS.SetBuffer(boidsGridKernelHandle, "BoidRW", cbBoid[0]);
        boidCS.SetBuffer(boidsGridKernelHandle, "GridIndicesRO", cbGridIndices);
        //忘了这个！调用的函数用到了它
        boidCS.SetBuffer(boidsGridKernelHandle,"BoidsCenterRW",cbBoidsCenter);
        boidCS.Dispatch(boidsGridKernelHandle, boidsCount / SIMULATION_BLOCK_SIZE, 1, 1);
    }

    void OnRenderObject()
    {
        
        for (int i = 0; i < boidsCount; i++)
        {
            material.SetPass(0);
            material.SetInt("_boidID",i);
            material.SetBuffer("_boidBuffer", cbBoid[0]);
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
        }     
    }

}
