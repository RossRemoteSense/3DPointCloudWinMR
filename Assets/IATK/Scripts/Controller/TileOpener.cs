using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

public class TileOpener : MonoBehaviour
{
    public TextAsset XML_Tile_File;
    string directory = @"C:\Users\uqmcord1\Documents\Research Projects\CoM_Point_Cloud_2018_LAS\LAZ\";

    [SerializeField]
    public bool LoadOneTile;
    [SerializeField]
    public bool CreateColorHistogram;
    public int TileToLoad;

    [SerializeField]
    public float minZ;

    [SerializeField]
    public float maxZ;

    [SerializeField]
    public int sampling;

    public struct Tile
    {
        public int x;
        public int y;
        public string fileName;
    }

    int[][] Grid;
    List<Tile> tiles = new List<Tile>();

    private List<PointcloudVisualiser> pointCloudVisualisers = new List<PointcloudVisualiser>();
    private List<PointcloudVisualiser> loadingPointCloudVisualisers = new List<PointcloudVisualiser>();

    [SerializeField]
    public int bins;

    void openXML_Tile_File()
    {
        string textContent = XML_Tile_File.text;
        string[] lines = textContent.Split(new char[] { '\r' });
        //format of the tiles: Tile_+004_+003

        MatchCollection mc = Regex.Matches(textContent, @"Tile_\+[0-9][0-9][0-9]_\+[0-9][0-9][0-9]");

        for (int i = 0; i < mc.Count; i++)
        {
            MatchCollection gridPos = Regex.Matches(mc[i].Value, @"\d+");
            Tile t = new Tile() { x = int.Parse(gridPos[0].Value), y = int.Parse(gridPos[1].Value), fileName = mc[i].Value+".laz" };
            tiles.Add(t);
        }
    }

    void debugGrid()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            //tile.AddComponent<PointcloudVisualiser>();
            //tile.GetComponent<PointcloudVisualiser>().FileName = directory + tiles[i].fileName;
            //tile.GetComponent<PointcloudVisualiser>().startReadLazThread();
            //tile.GetComponent<PointcloudVisualiser>().createPointCloud();

            //tile.transform.RotateAround(Vector3.right, Mathf.PI / 2f);
            tile.transform.position = new Vector3(tiles[i].x,0, tiles[i].y );
        }
    }

    void LoadSingleTile(int tileN)
    {
        StartCoroutine(AsyncLoadTiles(new List<Tile>() { tiles[tileN] }));
    }

    void LoadAllTiles()
    {
        StartCoroutine(AsyncLoadTiles(tiles));
    }

    IEnumerator AsyncLoadTiles(List<Tile> tiles)
    {
        foreach (var t in tiles)
        {
            var path = directory + t.fileName;
            if (System.IO.File.Exists(path))
            {
                GameObject tile = new GameObject();
                var pcv = tile.AddComponent<PointcloudVisualiser>();
                pcv.MyTileOpener = this;

                pcv.FileName = directory + t.fileName;
                pcv.dataResolution = sampling;
                pcv.minZ = minZ;
                pcv.maxZ = maxZ;
                
                pointCloudVisualisers.Add(pcv);
                
                tile.transform.position = new Vector3(t.x, 0, t.y);    
            }
            yield return new WaitForEndOfFrame();
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
        openXML_Tile_File();

        if (LoadOneTile) LoadSingleTile(TileToLoad); else LoadAllTiles();

    }


    // Update is called once per frame
    void Update()
    {
        for (int i = pointCloudVisualisers.Count - 1; i >= 0; i--)
        {
            var pc = pointCloudVisualisers[i];
            if (pc.initState == PointcloudVisualiser.State.Uninitialised && loadingPointCloudVisualisers.Count == 0)
            {
                loadingPointCloudVisualisers.Add(pc);
                Thread t = new Thread(pc.Load);
                t.Start();
                pointCloudVisualisers.Remove(pc);
                break; 
            }
        }

        for (int i = 0; i <  loadingPointCloudVisualisers.Count; i++)
        {
            var pc = loadingPointCloudVisualisers[i];
            if (pc.initState == PointcloudVisualiser.State.Loaded)
            {
                pc.CreatePointCloudLODs();
                loadingPointCloudVisualisers.RemoveAt(i);
                break;
            }
            // pc.ReadLaz();
            
        }
    }
}
