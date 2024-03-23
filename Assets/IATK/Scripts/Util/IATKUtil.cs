using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Added this line to fix the error

namespace IATK
{
    public class IATKUtil
    {
        private static Texture2D[] ordinaryTextures;
        private static GameObject objectToAddTextureTo;
        private static Texture2DArray texture2DArray;

        private static void CreateTextureArray(string fileName)
        {

            string finalFileName = System.IO.Path.GetFileName(fileName);

            // Specify the directory containing the csv file
            string csvFilePath = Application.dataPath + "/Resources/" + finalFileName + ".csv";

            // Read all lines from the csv file
            string[] lines = System.IO.File.ReadAllLines(csvFilePath);

            // Initialize the ordinaryTextures array
            ordinaryTextures = new Texture2D[lines.Length - 1]; // Subtract 1 to exclude the header line

            // Load each .png file as a texture and store it in the ordinaryTextures array
            for (int i = 1; i < lines.Length; i++) // Start from 1 to exclude the header line
            {
                string[] lineData = lines[i].Split(',');
                string filePath = Application.dataPath + "/Resources/mnist_img/" + lineData[4]; // Assuming the filename is in the fourth column
                byte[] fileData = System.IO.File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                ordinaryTextures[i - 1] = tex; // Subtract 1 to account for the header line
            }

            // Create Texture2DArray
            texture2DArray = new
                Texture2DArray(ordinaryTextures[0].width,
                ordinaryTextures[0].height, ordinaryTextures.Length,
                TextureFormat.RGBA32, true, false);

            // Apply settings
            texture2DArray.filterMode = FilterMode.Bilinear;
            texture2DArray.wrapMode = TextureWrapMode.Repeat;

            // Loop through ordinary textures and copy pixels to the
            // Texture2DArray
            for (int i = 0; i < ordinaryTextures.Length; i++)
            {
                texture2DArray.SetPixels(ordinaryTextures[i].GetPixels(0), i, 0);
            }

            // Apply our changes
            texture2DArray.Apply();
            // Set the texture to a material
        }
        /// returns a View with the specific geometry configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static Material GetMaterialFromTopology(AbstractVisualisation.GeometryType configuration, string fileName)
        {
            Material mt = null;

            CreateTextureArray(fileName);

            switch (configuration)
            {
                case AbstractVisualisation.GeometryType.Undefined:
                    return null;
                case AbstractVisualisation.GeometryType.Points:
                    mt = new Material(Shader.Find("IATK/OutlineDots"));
                    mt.SetTexture("_MainTex", texture2DArray);
                    mt.renderQueue = 3000;
                    return mt;
                case AbstractVisualisation.GeometryType.Lines:                   
                    mt = new Material(Shader.Find("IATK/LinesShader"));
                    mt.renderQueue = 3000;
                    return mt;
                case AbstractVisualisation.GeometryType.Quads:                   
                    mt = new Material(Shader.Find("IATK/Quads"));
                    mt.renderQueue = 3000;
                    return mt;
                case AbstractVisualisation.GeometryType.LinesAndDots:
                    mt = new Material(Shader.Find("IATK/LineAndDotsShader"));
                    mt.renderQueue = 3000;
                    return mt;
                case AbstractVisualisation.GeometryType.Cubes:
                    mt = new Material(Shader.Find("IATK/CubeShader"));
                    mt.renderQueue = 3000;
                    return mt;
                case AbstractVisualisation.GeometryType.Bars:
                    mt = new Material(Shader.Find("IATK/BarShader"));
                    mt.renderQueue = 3000;
                    return mt;
                case AbstractVisualisation.GeometryType.Spheres:
                    mt = new Material(Shader.Find("IATK/SphereShader"));
                    mt.mainTexture = Resources.Load("sphere-texture") as Texture2D;
                    mt.renderQueue = 3000;
                    return mt;
                default:
                    return null;
            }
        }

    }
}
