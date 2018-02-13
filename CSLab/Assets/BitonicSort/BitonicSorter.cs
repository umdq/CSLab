using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BitonicSorter 
{
    // The number of elements to sort is limited to an even power of 2
    // At minimum 8,192 elements - BITONIC_BLOCK_SIZE * TRANSPOSE_BLOCK_SIZE
    // At maximum 262,144 elements - BITONIC_BLOCK_SIZE * BITONIC_BLOCK_SIZE
    readonly int BITONIC_BLOCK_SIZE = 512;
    readonly int TRANSPOSE_BLOCK_SIZE = 16;
    readonly int MATRIX_WIDTH = 512;// = BITONIC_BLOCK_SIZE;
    private int NUM_ELEMENTS;
    private int MATRIX_HEIGHT;
    private int[] srcData;

    private ComputeShader cshader;
    private ComputeBuffer cbData;
    private int kernelSort;
    private ComputeBuffer cbTransposed;
    private int kernelTranspose;

    delegate void GPUSortDelegate();
    private GPUSortDelegate GPUSortDel;//随NUM_ELEMENTS变化

    public BitonicSorter(int[] src,ComputeShader cs)
    {
        cshader = cs;
        kernelSort = cshader.FindKernel("BitonicSort");
        kernelTranspose = cshader.FindKernel("MatrixTranspose");
         
        SetData(src);
    }

    //used to initialize & change src data
    public void SetData(int[] src)
    {
        srcData = src;
        NUM_ELEMENTS = srcData.Length;
        MATRIX_HEIGHT = NUM_ELEMENTS / MATRIX_WIDTH;
        if (NUM_ELEMENTS <= 512)
        {
            GPUSortDel = GPUSortSmallNum;
        }
        else if (NUM_ELEMENTS < 8192)//width小于16
        {
            GPUSortDel = GPUSortMiddleNum;
        }
        else
        {
            GPUSortDel = GPUSortLargeNum;
        }

        int stride=4;//just<int>
        cbData= new ComputeBuffer(NUM_ELEMENTS, stride);
        cbTransposed = new ComputeBuffer(NUM_ELEMENTS, stride);
        cbData.SetData(srcData);
    }

    public void GPUSort()
    {
        GPUSortDel();
    }

    void GPUSortLargeNum()
    {
        // First sort the rows for the levels <= to the block size
        cshader.SetBuffer(kernelSort, "Data", cbData);
        for( int level = 2 ; level <= BITONIC_BLOCK_SIZE ; level = level * 2 )
        {
            cshader.SetInt("g_iLevel", level);
            cshader.SetInt("g_iLevelMask", level);
            cshader.Dispatch( kernelSort,NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1 );
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for( int level = (BITONIC_BLOCK_SIZE * 2) ; level <= NUM_ELEMENTS ; level = level * 2 )
        {
            // Transpose
            cshader.SetInt("g_iWidth",MATRIX_WIDTH);
            cshader.SetInt("g_iHeight", MATRIX_HEIGHT);
            cshader.SetBuffer(kernelTranspose, "Input", cbData);
            cshader.SetBuffer(kernelTranspose, "Data", cbTransposed);
            cshader.Dispatch(kernelTranspose ,MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, 1 );

            // Sort the transposed column data
            cshader.SetInt("g_iLevel", level / BITONIC_BLOCK_SIZE);
            cshader.SetInt("g_iLevelMask", (level & ~NUM_ELEMENTS) / BITONIC_BLOCK_SIZE);//待改为有选择最终升or降
            cshader.SetBuffer(kernelSort, "Data", cbTransposed);
            cshader.Dispatch(kernelSort ,NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1 );

            // Transpose回
            cshader.SetInt("g_iWidth",MATRIX_HEIGHT);
            cshader.SetInt("g_iHeight", MATRIX_WIDTH);
            cshader.SetBuffer(kernelTranspose, "Input", cbTransposed);
            cshader.SetBuffer(kernelTranspose, "Data",cbData);
            cshader.Dispatch(kernelTranspose ,MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, 1 );

            // Sort the row data
            cshader.SetInt("g_iLevel", BITONIC_BLOCK_SIZE);
            cshader.SetInt("g_iLevelMask", level);
            cshader.SetBuffer(kernelSort, "Data", cbData);
            cshader.Dispatch( kernelSort,NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1 );
        }
        //download the data
        //cbData.GetData(srcData);
    }

    void GPUSortSmallNum()
    {
        cshader.SetBuffer(kernelSort, "Data", cbData);
        for( int level = 2 ; level <= NUM_ELEMENTS ; level = level * 2 )
        {
            cshader.SetInt("g_iLevel", level);
            cshader.SetInt("g_iLevelMask", level);
            cshader.Dispatch( kernelSort,NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1 );
        }
    }

    void GPUSortMiddleNum()//暴力转置，不使用group。再试试干脆不转置，sort也不用group，一次dispatch完全搞定…
    {
        
    }

    public void DownloadToCPU()
    {
        cbData.GetData(srcData);
    }

    //GarbageCollector disposing of ComputeBuffer
    public void ReleaseCB()
    {
        cbData.Release();
        cbTransposed.Release();
    }
}
