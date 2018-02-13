using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GenerateMassiveRandomInt : MonoBehaviour {
    public int numElements = 16384;
    public ComputeShader cs;

    int[] srcData;//for quik sort
    int[] srcCopy;//for bitonic sort
    BitonicSorter bs;
	
    //.net对高精度计时WIN API QueryPerformanceCounter/Frequency的封装
    System.Diagnostics.Stopwatch watch=new System.Diagnostics.Stopwatch();

	void Start ()
    {
        srcData = new int[numElements];
        GenerateRandomSrc();
        bs = new BitonicSorter(srcCopy,cs);

        QuikSortTest();
        BitonicSortTest();
	}
	
    void GenerateRandomSrc()
    {
        //Debug.Log("start to generate source data...");
        //FileStream fs = new FileStream("SrcRandomData.txt", FileMode.Create, FileAccess.Write);
        //StreamWriter sw = new StreamWriter(fs);
        using (StreamWriter sw = new StreamWriter("SrcRandomData.txt"))
        {
            for (int i = 0; i < numElements; i++)
            {
                srcData[i] = Random.Range(0,500000);
                sw.Write(srcData[i] + " ");
                if (i % 10 == 0 && i>0)
                    sw.WriteLine();
            }
        }
        srcCopy=(int[])srcData.Clone();
        //Debug.Log("finish generating source data");
    }

    void QuikSortTest()
    {
        watch.Start();
        //quik sort
        System.Array.Sort(srcData);
        watch.Stop();
        double runTime = watch.Elapsed.TotalMilliseconds;
        Debug.Log("C#自带的快排用时：" + runTime+ "ms");
        using (StreamWriter sw = new StreamWriter("QuikSortResult.txt"))
        {
            sw.WriteLine("数据量：{0}，C#自带的快排用时：{1}ms", numElements,runTime);
            WriteArrayToFile(sw, srcData);
        }
    }

    void WriteArrayToFile(StreamWriter sw,int[] data)
    {
        for (int i=0;i<data.Length;i++)
        {
            sw.Write(data[i]+" ");
            if (i % 10 == 0 && i>0)
                sw.WriteLine();
        }
    }

    void BitonicSortTest()
    {
        watch.Start();
        bs.GPUSort();
        watch.Stop();
        double runTime = watch.Elapsed.TotalMilliseconds;
        Debug.Log("并行双调排序用时：" + runTime+ "ms");
        bs.DownloadToCPU();//update srcCopy
        using (StreamWriter sw = new StreamWriter("BitonicSortResult.txt"))
        {
            sw.WriteLine("数据量：{0}，并行双调排序用时：{1}ms",numElements, runTime);
            WriteArrayToFile(sw, srcCopy);
        }
    }

}
