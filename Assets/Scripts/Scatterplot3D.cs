using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IATK;
using System.Linq;

public class Scatterplot3D : MonoBehaviour
{
    //todo: load data from folder for study...
    public TextAsset Data;

    public BrushingAndLinkingViews BrushingAndLinkingViews;

    [SerializeField]
    public string xAxis;
    [SerializeField]
    public string yAxis;
    [SerializeField]
    public string zAxis;

    public int segNum;

    //IATK objects
    CSVDataSource csvdata;
    View v;

    // Start is called before the first frame update
    void Start()
    {
        csvdata = createCSVDataSource(Data.text);
        v = PointCloud(csvdata);

        //adding the view to brushing and linking object
        BrushingAndLinkingViews.brushingVisualisations.Add(v);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ************** PRIVATE UTIL METHODS FOR 3D POINTCLOUD VIS ***************************

    CSVDataSource createCSVDataSource(string data)
    {
        CSVDataSource dataSource;
        dataSource = gameObject.AddComponent<CSVDataSource>();
        dataSource.load(data, null);
        return dataSource;
    }

    View PointCloud(CSVDataSource csvds)
    {
        // create a view builder with the point topology
        ViewBuilder vb = new ViewBuilder(MeshTopology.Points, "3D Point Cloud").
        initialiseDataView(csvds.DataCount).
        setDataDimension(csvds[xAxis].Data, ViewBuilder.VIEW_DIMENSION.X).
        setDataDimension(csvds[yAxis].Data, ViewBuilder.VIEW_DIMENSION.Y).
        setDataDimension(csvds[zAxis].Data, ViewBuilder.VIEW_DIMENSION.Z).
        createIndicesPointTopology().setSingleColor(Color.white);
        //setColors(cols).


        //setColors(csvds["Time"].Data.Select(x => g.Evaluate(x)).ToArray());

        // initialise the view builder wiith thhe number of data points and parent GameOBject

        //Enumerable.Repeat(1f, dataSource[0].Data.Length).ToArray()
        Material mt = IATKUtil.GetMaterialFromTopology(AbstractVisualisation.GeometryType.Points, Data.name);
        mt.SetFloat("_MinSize", 0.01f);
        mt.SetFloat("_MaxSize", 0.05f);
        mt.SetFloat("_Size", 0.15f);

        View v = vb.updateView().apply(gameObject, mt);
        return v;
    }
}

