using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IATK;
using System.Linq;
using laszip.net;
using System.Threading;
//using Accord.Math;

public class PointcloudVisualiser : MonoBehaviour
{
    public TextAsset pointcloudData;
    public string FileName;

    public int sampling;

    public int x;
    public int y;
    public int z;
    public int i;
    public int r;
    public int g;
    public int b;

    public struct PointCloud
    {
        public float x;
        public float y;
        public float z;
        public float i;
        public float r;
        public float g;
        public float b;
    }

    PointCloud[] PointCloudDataArray;
    Color[] ColorArray;

    float[] xs;
    float[] ys;
    float[] zs;
    
    public int bins;

    public int dataResolution;

    // Thread for loading data
    Thread loader;

    //Thread for building the mesh
    Thread pointCloudInstanciation;

    //handle for data
    public TileOpener MyTileOpener;

    public enum State
    {
        Uninitialised,
        Loaded,
        Normalised,
        Built
    }

    public float minZ, maxZ;

    public State initState = State.Uninitialised;
    
    
    public void startReadLazThread()
    {
        loader = new Thread(ReadLaz);
        loader.Start();
    }

    public void startPointCloudCreation()
    {
        //pointCloudInstanciation = new Thread(createPointCloudLASView(minZ, maxZ));
        //pointCloudInstanciation.Start();
    }

    public void Load()
    {
        
        ReadLaz();
        Normalise();

        
        initState = State.Loaded;
    }

    public void ReadLaz()
    {
        var lazReader = new laszip_dll();
        var compressed = true;
        lazReader.laszip_open_reader(FileName, ref compressed);
        var numberOfPoints = lazReader.header.number_of_point_records;
        PointCloudDataArray = new PointCloud[numberOfPoints];

        int classification = 0;
        var point = new PointCloud();
        var coordArray = new double[6];

        // Loop through number of points indicated
        for (int pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
        {
            if ((pointIndex % 1000) == 0)
            {
                //Debug.Log("loading " + pointIndex);
            }

            // Read the point
            lazReader.laszip_read_point();

            // Get precision coordinates
            lazReader.laszip_get_coordinates(coordArray);

            point.x = (float) coordArray[0];
            point.y = (float) coordArray[1];
            point.z = (float) coordArray[2];
            point.r = (float) lazReader.point.rgb[0];
            point.g = (float) lazReader.point.rgb[1];
            point.b = (float) lazReader.point.rgb[2];

            // Get classification value
            classification = lazReader.point.classification;

            PointCloudDataArray[pointIndex] = point;

        }

        // Close the reader
        lazReader.laszip_close_reader();
    }

    protected void Normalise()
    {
        ColorArray = new Color[PointCloudDataArray.Length];

        float maxR = float.MinValue;
        float maxG = float.MinValue;
        float maxB = float.MinValue;
        float MaxX = float.MinValue, MinX = float.MaxValue, MaxY = float.MinValue, MinY = float.MaxValue;
        for (int i = 0; i < PointCloudDataArray.Length; ++i)
        {
            MaxX = Mathf.Max(PointCloudDataArray[i].x, MaxX);
            MinX = Mathf.Min(PointCloudDataArray[i].x, MinX);
            MaxY = Mathf.Max(PointCloudDataArray[i].y, MaxY);
            MinY = Mathf.Min(PointCloudDataArray[i].y, MinY);
            maxR = Mathf.Max(PointCloudDataArray[i].r, maxR);
            maxG = Mathf.Max(PointCloudDataArray[i].g, maxG);
            maxB = Mathf.Max(PointCloudDataArray[i].b, maxB);
        }
        
        for (int i = 0; i < PointCloudDataArray.Length; ++i)
        {
            PointCloudDataArray[i].x = UtilMath.normaliseValue(PointCloudDataArray[i].x, MinX, MaxX, 0f, 1f);
            PointCloudDataArray[i].y = UtilMath.normaliseValue(PointCloudDataArray[i].y, MinY, MaxY, 0f, 1f);            
            PointCloudDataArray[i].z = UtilMath.normaliseValue(PointCloudDataArray[i].z, minZ, maxZ, 0f, 1f); 
            
            ColorArray[i] = new Color(PointCloudDataArray[i].r / maxR, 
                                      PointCloudDataArray[i].g / maxG, 
                                      PointCloudDataArray[i].b / maxB);
        }
    }

    public void CreatePointCloudLODs()
    {
        var lodgroup = gameObject.AddComponent<LODGroup>();

        int[] lodLevels = { 50000, 100000, 500000 };

        List<Renderer> lodRenderers = new List<Renderer>();
        int start = 0;
        foreach (var length in lodLevels)
        {
            lodRenderers.Add(CreatePointCloudLASView(start, start + length));
            start += length;
        }

        LOD[] lods = new LOD[lodLevels.Length];

        for (int i = 0; i < lodRenderers.Count(); ++i)
        {
            var renderers = lodRenderers.GetRange(0, lodRenderers.Count() - i).ToArray();
            lods[i] = new LOD(1.0f / (i + 2), renderers);
        }
        
        lodgroup.SetLODs(lods);
        lodgroup.RecalculateBounds();

        initState = State.Built;
    }

    protected Renderer CreatePointCloudLASView(int start, int length)
    {
        var watch = new System.Diagnostics.Stopwatch();

        watch.Start();

        var pointCloudSubset = PointCloudDataArray.Skip(start).Take(length);

        ViewBuilder vb = new ViewBuilder(MeshTopology.Points, "Pointcloud View").
            initialiseDataView(length).
            setDataDimension(pointCloudSubset.Select(v => v.x), ViewBuilder.VIEW_DIMENSION.X).
            setDataDimension(pointCloudSubset.Select(v => v.z), ViewBuilder.VIEW_DIMENSION.Y).
            setDataDimension(pointCloudSubset.Select(v => v.y), ViewBuilder.VIEW_DIMENSION.Z).
            setColors(ColorArray.Skip(start).Take(length).ToArray()).          
            setSize(Enumerable.Repeat(0.001f, length).ToArray());

        Material mt = IATKUtil.GetMaterialFromTopology(AbstractVisualisation.GeometryType.Points, "");

        mt.SetFloat("_MinSize", 0.00001f);
        mt.SetFloat("_MaxSize", 0.00025f);
        mt.SetFloat("_Size", 0.02f);

        View view = vb.updateView().apply(gameObject, mt);
        watch.Stop();

        Debug.Log("Execution Time: " + watch.ElapsedMilliseconds);
      
        return view.GetComponentInChildren<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        //if(finishedLoading && PointCloudDataArray != null)
        //{
        //    startPointCloudCreation();
        //    finishedLoading = false;
        //}
    }
}
