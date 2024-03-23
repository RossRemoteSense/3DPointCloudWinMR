using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class cubecolor : MonoBehaviour
{
    public int cubeSize;
    
    // Start is called before the first frame update
    void Start()
    {
        StreamWriter fs = new StreamWriter("Assets\\cubecolor.txt");
        fs.WriteLine("x,y,z,r,g,b");
        for (int i = 0; i < cubeSize; i++)
        {
            for (int j = 0; j < cubeSize; j++)
            {
                for (int k = 0; k < cubeSize; k++)
                {
/*                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = new Vector3(i, j, k);
                    go.GetComponent<Renderer>().material.color = new Color((float)i / (float)cubeSize, (float)j / (float)cubeSize, (float)k / (float)cubeSize);*/
                    fs.WriteLine(((float)i).ToString("G6")+","+ ((float)j).ToString("G6") + ","+ ((float)k).ToString("G6") + ","+(100f*(float)i / (float)cubeSize).ToString("G6") + "," + (100f * (float)j / (float)cubeSize).ToString("G6") + "," + (100f * (float)k / (float)cubeSize).ToString("G6"));
                }
            }
        }

        fs.Close();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
