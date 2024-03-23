using IATK;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Linq;
using UnityEngine.Rendering;

public class BrushingAndLinkingViews : MonoBehaviour
{

    [SerializeField]
    public ComputeShader computeShader;
    [SerializeField]
    public Material myRenderMaterial;

    [SerializeField]
    public List<View> brushingVisualisations;
    [SerializeField]
    public List<LinkingVisualisations> brushedLinkingVisualisations;

    [SerializeField]
    public bool isBrushing;

   // [Serializable]
    public enum PointClass
    {
        CLASS1= 3,
        CLASS2 = 40,
        CLASS3 = 80,
    }

    [SerializeField]
    public PointClass _PointClass = PointClass.CLASS1;

    [Range(0f, 1f)]
    public float brushRadius;
    [SerializeField]
    public bool showBrush = false;
    [SerializeField]
    [Range(1f, 10f)]
    public float brushSizeFactor = 1f;

    [SerializeField]
    public Transform input1;
    [SerializeField]
    public Transform input2;

    [SerializeField]
    public BrushType BRUSH_TYPE;
    public enum BrushType
    {
        SPHERE = 0,
        BOX
    };

    [SerializeField]
    public SelectionType SELECTION_TYPE;
    public enum SelectionType
    {
        FREE = 0,
        ADD,
        SUBTRACT
    }

    [SerializeField]
    List<float> brushedIndices;

    [SerializeField]
    public Material debugObjectTexture;

    private int kernelComputeBrushTexture;
    private int kernelComputeBrushedIndices;

    private RenderTexture brushedIndicesTexture;
    private int texSize;

    private ComputeBuffer dataBuffer;
    private ComputeBuffer filteredIndicesBuffer;
    private ComputeBuffer brushedIndicesBuffer;

    private bool hasInitialised = false;
    private bool hasFreeBrushReset = false;
    private AsyncGPUReadbackRequest brushedIndicesRequest;

    private ComputeBuffer debugBuffer;
    private ComputeBuffer debugBuffer2;

    [Range(0f,1f)]
    public float classPPDebug; 

    private void Start()
    {
        InitialiseShaders();

        debugBuffer = new ComputeBuffer(100, sizeof(float) * 4); // Adjust size and stride
        computeShader.SetBuffer(kernelComputeBrushTexture, "debugBuffer", debugBuffer);

        debugBuffer2 = new ComputeBuffer(100, sizeof(float) * 4); // Adjust size and stride
        computeShader.SetBuffer(kernelComputeBrushTexture, "debugBuffer2", debugBuffer2);

     //   bigMeshVertices = v.BigMesh.getBigMeshVertices();
    }

    /// <summary>
    /// Initialises the indices for the kernels in the compute shader.
    /// </summary>
    private void InitialiseShaders()
    {
        kernelComputeBrushTexture = computeShader.FindKernel("CSMain");
        kernelComputeBrushedIndices = computeShader.FindKernel("ComputeBrushedIndicesArray");
        UnityEngine.Debug.Log("In Init");
    }

    /// <summary>
    /// Initialises the buffers and textures necessary for the brushing and linking to work.
    /// </summary>
    /// <param name="dataCount"></param>
    private void InitialiseBuffersAndTextures(int dataCount)
    {
        dataBuffer = new ComputeBuffer(dataCount, 12);
        dataBuffer.SetData(new Vector3[dataCount]);
        computeShader.SetBuffer(kernelComputeBrushTexture, "dataBuffer", dataBuffer);

        filteredIndicesBuffer = new ComputeBuffer(dataCount, 4);
        filteredIndicesBuffer.SetData(new float[dataCount]);
        computeShader.SetBuffer(kernelComputeBrushTexture, "filteredIndicesBuffer", filteredIndicesBuffer);

        brushedIndicesBuffer = new ComputeBuffer(dataCount, 4);
        brushedIndicesBuffer.SetData(Enumerable.Repeat(-1, dataCount).ToArray());
        computeShader.SetBuffer(kernelComputeBrushedIndices, "brushedIndicesBuffer", brushedIndicesBuffer);

        texSize = NextPowerOf2((int)Mathf.Sqrt(dataCount));
        brushedIndicesTexture = new RenderTexture(texSize, texSize, 32, RenderTextureFormat.ARGB32);
        
        brushedIndicesTexture.enableRandomWrite = true;
        brushedIndicesTexture.filterMode = FilterMode.Point;
        brushedIndicesTexture.Create();

        myRenderMaterial.SetTexture("_MainTex", brushedIndicesTexture);
        
        Matrix4x4 mvpMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix * transform.localToWorldMatrix;
        computeShader.SetMatrix("W_Matrix", mvpMatrix);

        UnityEngine.Debug.Log("mvpMatrix: " + mvpMatrix);
        computeShader.SetFloat("_size", texSize);

        computeShader.SetTexture(kernelComputeBrushTexture, "Result", brushedIndicesTexture);
        computeShader.SetTexture(kernelComputeBrushedIndices, "Result", brushedIndicesTexture);

        hasInitialised = true;
    }

    public struct ViewData {
        public Vector3[] bigMeshVertices;
        public float[] filterChannel;
        public int id;
    };

    List<ViewData> localViewData = new List<ViewData>();

    /// <summary>
    /// Updates the computebuffers with the values specific to the currently brushed visualisation.
    /// </summary>
    /// <param name="v"></param>
    public void UpdateComputeBuffers(View v)
    {

        //lookup the data in the list of ViewData, if not there already, create it
        Vector3[] _bigMeshVertices;
        float[] _filterChannel;

        int instanceId = v.GetInstanceID();

        if(localViewData.Any(x=>x.id==instanceId))
        {
            _bigMeshVertices = localViewData.Single(x => x.id == instanceId).bigMeshVertices;
            _filterChannel = localViewData.Single(x => x.id == instanceId).filterChannel;
        }
        else
        {
            ViewData vd = new ViewData()
            {
                bigMeshVertices = v.BigMesh.getBigMeshVertices(),
                filterChannel = v.GetFilterChannel(),
                id = v.GetInstanceID()
            };

            localViewData.Add(vd);

            _bigMeshVertices = vd.bigMeshVertices;
            _filterChannel = vd.filterChannel;
        }

        //if (bigMeshVertices == null)
        //{
        //    bigMeshVertices = v.BigMesh.getBigMeshVertices();
        //}

        //if (filterChannel == null)
        //{
        //    filterChannel = v.GetFilterChannel();
        //}

        dataBuffer.SetData(_bigMeshVertices);
        computeShader.SetBuffer(kernelComputeBrushTexture, "dataBuffer", dataBuffer);

        filteredIndicesBuffer.SetData(_filterChannel);// BigMesh.GetFilterChannel());
        computeShader.SetBuffer(kernelComputeBrushTexture, "filteredIndicesBuffer", filteredIndicesBuffer);
        
    }


    /// <summary>
    /// Finds the next power of 2 for a given number.
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private int NextPowerOf2(int number)
    {
        int pos = 0;

        while (number > 0)
        {
            pos++;
            number = number >> 1;
        }
        return (int)Mathf.Pow(2, pos);
    }

    public void Update()
    {
       // var val = Convert.ChangeType(_PointClass, _PointClass.GetTypeCode());
        //print(val/10f);
        if (isBrushing && brushingVisualisations.Count > 0 && input1 != null && input2 != null)
        {
            if (hasInitialised)
            {
                UpdateBrushTexture();

             //   UpdateBrushedIndices();
            }
            else
            {
                InitialiseBuffersAndTextures(brushingVisualisations[0].BigMesh.NB_VERTTICES);
            }
        }

    }

    /// <summary>
    /// Returns a list with all indices - if index > 0, index is brushed. It's not otherwise
    /// </summary>
    /// <returns></returns>
    public List<Vector2> GetBrushedIndices()
    {

        UpdateBrushedIndices();
        List<Vector2> indicesBrushed = new List<Vector2>();

        for (int i = 0; i < brushedIndices.Count; i++)
        {

            if (brushedIndices[i] > 0)
            {
                indicesBrushed.Add(new Vector2(i, brushedIndices[i]));
                print(brushedIndices[i].ToString("G6"));
            }
        }

        return indicesBrushed;
    }

    /// <summary>
    /// Updates the brushedIndicesTexture using the visualisations set in the brushingVisualisations list.
    /// </summary>
    private void UpdateBrushTexture()
    {
        Vector3 projectedPointer1;
        Vector3 projectedPointer2;

        computeShader.SetInt("BrushMode", (int)BRUSH_TYPE);
        computeShader.SetInt("SelectionMode", (int)SELECTION_TYPE);


        //        computeShader.SetFloat("PointClass", classPPDebug);

        switch (_PointClass)
        {
            case PointClass.CLASS1:
                computeShader.SetFloat("PointClass", 0.03f);
                break;
            case PointClass.CLASS2:
                computeShader.SetFloat("PointClass", 0.4f);
                break;
            case PointClass.CLASS3:
                computeShader.SetFloat("PointClass", 0.8f);
                break;
            default:
                break;
        }


        //print((float)_PointClass / 10f);
        hasFreeBrushReset = false;

        foreach (var vis in brushingVisualisations)
        {
            UpdateComputeBuffers(vis);

            switch (BRUSH_TYPE)
            {
                case BrushType.SPHERE:
                    //projectedPointer1 = vis.transform.InverseTransformPoint(input1.transform.position);
                    projectedPointer1 = Camera.main.transform.InverseTransformPoint(input1.transform.position);
                    UnityEngine.Debug.Log($"pointer1: {projectedPointer1.ToString()}");
                    computeShader.SetFloats("pointer1", projectedPointer1.x, projectedPointer1.y, projectedPointer1.z);

                    break;
                case BrushType.BOX:
                    projectedPointer1 = vis.transform.InverseTransformPoint(input1.transform.position);
                    projectedPointer2 = vis.transform.InverseTransformPoint(input2.transform.position);

                    computeShader.SetFloats("pointer1", projectedPointer1.x, projectedPointer1.y, projectedPointer1.z);
                    computeShader.SetFloats("pointer2", projectedPointer2.x, projectedPointer2.y, projectedPointer2.z);
                    break;
                default:
                    break;
            }

            //set the filters and normalisation values of the brushing visualisation to the computer shader
            computeShader.SetFloat("_MinNormX", 0);
            computeShader.SetFloat("_MaxNormX", 1);
            computeShader.SetFloat("_MinNormY", 0);
            computeShader.SetFloat("_MaxNormY", 1);
            computeShader.SetFloat("_MinNormZ", 0);
            computeShader.SetFloat("_MaxNormZ", 1);

            computeShader.SetFloat("_MinX", 0);
            computeShader.SetFloat("_MaxX", 1);
            computeShader.SetFloat("_MinY", 0);
            computeShader.SetFloat("_MaxY", 1);
            computeShader.SetFloat("_MinZ", 0);
            computeShader.SetFloat("_MaxZ", 1);

            // Ross Brown - set the MVP matrix to the shader
            computeShader.SetFloat("RadiusSphere", brushRadius);

            computeShader.SetFloat("width", 1);
            computeShader.SetFloat("height", 1);
            computeShader.SetFloat("depth", 1);
            Matrix4x4 mvpMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix * transform.localToWorldMatrix;
            computeShader.SetMatrix("W_Matrix", mvpMatrix);
            UnityEngine.Debug.Log("mvpMatrix: " + mvpMatrix);

            // Tell the shader whether or not the visualisation's points have already been reset by a previous brush, required to allow for
            // multiple visualisations to be brushed with the free selection tool
            if (SELECTION_TYPE == SelectionType.FREE)
                computeShader.SetBool("HasFreeBrushReset", hasFreeBrushReset);

            // Run the compute shader
            
            // Execute the shader
            computeShader.Dispatch(kernelComputeBrushTexture, Mathf.CeilToInt(texSize / 32f), Mathf.CeilToInt(texSize / 32f), 1);
            //ComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ);

            GraphicsFence fence = UnityEngine.Graphics.CreateGraphicsFence(UnityEngine.GraphicsFenceType.AsyncQueueSynchronization, SynchronisationStage.PixelProcessing);
            UnityEngine.Graphics.WaitOnAsyncGraphicsFence(fence);

            // Read back the data
            float[] debugData = new float[100 * 4]; // Adjust accordingly
            debugBuffer.GetData(debugData);

            float[] debugData2 = new float[100 * 4]; // Adjust accordingly
            debugBuffer2.GetData(debugData2);

            //UnityEngine.Debug.Log("Debug Data: " + debugData[0].x + " " + debugData[0].y + " " + debugData[0].z + " " + debugData[3].w);
            UnityEngine.Debug.Log($"First float4 in debugBuffer: {debugData[0]:F} {debugData[1]:F} {debugData[2]:F} {debugData[3]:F}");
            UnityEngine.Debug.Log($"Second float4 in debugBuffer2: {debugData2[0]:F} {debugData2[1]:F} {debugData2[2]:F} {debugData2[3]:F}");

            vis.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", brushedIndicesTexture);
            vis.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
            vis.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
            vis.BigMesh.SharedMaterial.SetFloat("showBrush", Convert.ToSingle(showBrush));
           // vis.BigMesh.SharedMaterial.SetColor("brushColor", brushColor[0]);

            hasFreeBrushReset = true;
        }
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        // Get values from request
        if (!brushedIndicesRequest.hasError)
        {
            brushedIndices = brushedIndicesRequest.GetData<float>().ToList();
            UnityEngine.Debug.Log("read the data");
        }
    }
    /// <summary>
    /// Updates the brushedIndices list with the currently brushed indices. A value of 1 represents brushed, -1 represents not brushed (boolean values are not supported).
    /// </summary>
    private void UpdateBrushedIndices()
    {
        computeShader.Dispatch(kernelComputeBrushedIndices, Mathf.CeilToInt(texSize / 32f), Mathf.CeilToInt(texSize / 32f), 1);

        // Dispatch again
        //computeShader.Dispatch(kernelComputeBrushedIndices, Mathf.CeilToInt(brushedIndicesBuffer.count / 32f), 1, 1);
        brushedIndicesRequest = AsyncGPUReadback.Request(brushedIndicesBuffer, OnCompleteReadback);

    }

    /// <summary>
    /// Releases the buffers on the graphics card.
    /// </summary>
    private void OnDestroy()
    {
        if (dataBuffer != null)
            dataBuffer.Release();

        if (filteredIndicesBuffer != null)
            filteredIndicesBuffer.Release();

        if (brushedIndicesBuffer != null)
            brushedIndicesBuffer.Release();
    }

    private void OnApplicationQuit()
    {
        if (dataBuffer != null)
            dataBuffer.Release();

        if (filteredIndicesBuffer != null)
            filteredIndicesBuffer.Release();

        if (brushedIndicesBuffer != null)
            brushedIndicesBuffer.Release();
    }
}