using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IATK;
using System.Linq;

public class HistogramView : MonoBehaviour
{
    public View myView { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /**********************************************/
    /*               histograms functions         */
    /**********************************************/

    public struct BinnedPixels
    {
        public int binId;
        public int pixelPosition;
    }
    public static BinnedPixels[] GetBinnedHistoPixels(float[] _pixels, int bins)
    {
        int[] histoDummy = new int[bins];

        BinnedPixels[] histogram =
            Enumerable.Range(0, _pixels.Length).Select(x => new BinnedPixels() { binId = 0, pixelPosition = 0 }).ToArray();

        for (int i = 0; i < _pixels.Length; i++)
        {
            try
            {
                histogram[i].pixelPosition = histoDummy[(int)(_pixels[i] * (float)(bins - 1))];
                histogram[i].binId = (int)(_pixels[i] * (float)(bins - 1));

                histoDummy[(int)(_pixels[i] * (float)(bins - 1))]++;
            }
            catch
            {
                // Debug.Log("could not project " + _pixels[i] + " ");
            }
        }

        return histogram;
    }

    public void CreateHistogramCurvature(Color[] pixels, BinnedPixels[] _pixels, int bins)
    {
        if (myView != null)
            foreach (Transform item in myView.GetComponentsInChildren<Transform>())
            {
                Destroy(item.gameObject);
            }


        float[] xDim = new float[_pixels.Length];
        float[] yDim = new float[_pixels.Length];
        List<int> indices = new List<int>();

        for (int i = 0; i < xDim.Length; i++)
        {
            xDim[i] = (float)_pixels[i].binId / (float)bins; //normalise bin values
            yDim[i] = (float)_pixels[i].pixelPosition + 0.4f;
            indices.Add(i);
        }

        //normalise pixel position height
        float minHist = yDim.Min();
        float maxHist = yDim.Max();

        //float[] normalisedHistogram = new float[_pixels.Length];

        for (int i = 0; i < _pixels.Length; i++)
        {
            yDim[i] =
                UtilMath.normaliseValue(yDim[i], minHist, maxHist, 0f, 1f);
        }

        ViewBuilder vb = new ViewBuilder(MeshTopology.Points, "Histogram luminance").
          initialiseDataView(_pixels.Length).
          setDataDimension(xDim, ViewBuilder.VIEW_DIMENSION.X).
          setDataDimension(yDim, ViewBuilder.VIEW_DIMENSION.Y).
          setColors(pixels).
          createIndicesPointTopology().
          setSize(xDim.Select(x => 0.001f).ToArray());

        Material mt = IATKUtil.GetMaterialFromTopology
            (AbstractVisualisation.GeometryType.Points, "");
        //mt.shader = Shader.Find("Perspective/PixelImmersiveShader");

        // mt = new Material(Shader.Find("Perspective/PixelImmersiveShader"));

        mt.SetFloat("_MinSize", 0.001f);
        mt.SetFloat("_MaxSize", 0.0025f);
        mt.SetFloat("_Size", 0.4f);
        //   vb.Indices = indices;

        View v = vb.updateView().apply(gameObject, mt);

        print("view histo : " + xDim.Length);

        myView = v;
    }

    public void CreateHistogramView(Color[] pixels, BinnedPixels[] _pixels, int bins)
    {
        if (myView != null)
            foreach (Transform item in myView.GetComponentsInChildren<Transform>())
            {
                Destroy(item.gameObject);
            }
        

        float[] xDim = new float[_pixels.Length];
        float[] yDim = new float[_pixels.Length];
        List<int> indices = new List<int>();

        for (int i = 0; i < xDim.Length; i++)
        {
            xDim[i] = (float)_pixels[i].binId / (float)bins; //normalise bin values
            yDim[i] = (float)_pixels[i].pixelPosition + 0.4f;
            indices.Add(i);
        }

        //normalise pixel position height
        float minHist = yDim.Min();
        float maxHist = yDim.Max();

        //float[] normalisedHistogram = new float[_pixels.Length];

        for (int i = 0; i < _pixels.Length; i++)
        {
            yDim[i] =
                UtilMath.normaliseValue(yDim[i], minHist, maxHist, 0f, 1f);
        }

        ViewBuilder vb = new ViewBuilder(MeshTopology.Points, "Histogram luminance").
          initialiseDataView(_pixels.Length).
          setDataDimension(xDim, ViewBuilder.VIEW_DIMENSION.X).
          setDataDimension(yDim, ViewBuilder.VIEW_DIMENSION.Y).
          setColors(pixels).
          createIndicesPointTopology().
          setSize(xDim.Select(x => 0.001f).ToArray());

        Material mt = IATKUtil.GetMaterialFromTopology
            (AbstractVisualisation.GeometryType.Points, "");
        //mt.shader = Shader.Find("Perspective/PixelImmersiveShader");

        // mt = new Material(Shader.Find("Perspective/PixelImmersiveShader"));

        mt.SetFloat("_MinSize", 0.001f);
        mt.SetFloat("_MaxSize", 0.0025f);
        mt.SetFloat("_Size", 0.4f);
        //   vb.Indices = indices;

        View v = vb.updateView().apply(gameObject, mt);

        print("view histo : " + xDim.Length);

        myView = v;
    }

    float[] NormaliseArray(float[] toNorm)
    {
        float[] normalisedArray = new float[toNorm.Length];
        float min = toNorm.Min();
        float max = toNorm.Max();

        for (int i = 0; i < toNorm.Length; i++)
        {
            normalisedArray[i] = UtilMath.normaliseValue(toNorm[i], min, max, 0f, 1f);
        }

        return normalisedArray;
    }

    public void UpdateView(float[] xs, float[] ys, float[] zs)
    {
        //v.UpdateXPositions(xs);
        //MyView.UpdateXPositions(NormaliseArray(xs));
        //MyView.UpdateYPositions(NormaliseArray(ys));
        //MyView.UpdateZPositions(NormaliseArray(zs));
        //MyView.TweenPosition();

    }

    /*********************************************/
    /*                 color space funcs         */
    /********************************************/


    /// <summary>
    /// 
    /// Col
    /// </summary>
    /// <param name="colorChannel"></param>
    /// <returns></returns>
    public static float sRGBtoLin(float colorChannel)
    {
        // Send this function a decimal sRGB gamma encoded color value
        // between 0.0 and 1.0, and it returns a linearized value.

        if (colorChannel <= 0.04045f)
        {
            return colorChannel / 12.92f;
        }
        else
        {
            return Mathf.Pow((colorChannel + 0.055f) / 1.055f, 2.4f);
        }
    }

    public static float LuminanceY(float vR, float vG, float vB)
    {
        return (0.2126f * sRGBtoLin(vR) + 0.7152f * sRGBtoLin(vG) + 0.0722f * sRGBtoLin(vB));
    }

    public static float YtoLstar(float Y)
    {
        // Send this function a luminance value between 0.0 and 1.0,
        // and it returns L* which is "perceptual lightness"

        if (Y <= (216f / 24389f))
        {       // The CIE standard states 0.008856 but 216/24389 is the intent for 0.008856451679036
            return Y * (24389f / 27f);  // The CIE standard states 903.3, but 24389/27 is the intent, making 903.296296296296296
        }
        else
        {
            return Mathf.Pow(Y, (1f / 3f)) * 116f - 16f;
        }
    }

    public static float getHue(Color c)
    {

        System.Drawing.Color sysCol = System.Drawing.Color.FromArgb
            ((int)(c.a * 255f), (int)(c.r * 255f), (int)(c.g * 255f), (int)(c.b * 255f));

        return sysCol.GetHue() / 360f;
    }



    public static float getBrightness(Color c)
    {

        System.Drawing.Color sysCol = System.Drawing.Color.FromArgb
            ((int)(c.a * 255f), (int)(c.r * 255f), (int)(c.g * 255f), (int)(c.b * 255f));

        return sysCol.GetBrightness();
    }

    public static float getSaturation(Color c)
    {

        System.Drawing.Color sysCol = System.Drawing.Color.FromArgb
            ((int)(c.a * 255f), (int)(c.r * 255f), (int)(c.g * 255f), (int)(c.b * 255f));

        return sysCol.GetSaturation();
    }

    /*
     spherical color coordinates
         */

    public static float getTheta(float r, float g)
    {
        return Mathf.Atan(g / r);
    }

    public static float getPhi(float b, float radius)
    {
        return Mathf.Acos(b / radius);
    }

    public static float getRadius(float r, float g, float b)
    {
        return Mathf.Sqrt(Mathf.Pow(r, 2f) + Mathf.Pow(g, 2f) + Mathf.Pow(b, 2f));
    }

}
