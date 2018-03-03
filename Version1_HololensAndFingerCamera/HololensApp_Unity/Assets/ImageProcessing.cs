using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageProcessing : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    static Dictionary<int, float[]> gaussianKernels = new Dictionary<int, float[]>();

    static float[] GetGaussianKernel(int n)
    {
        if (!gaussianKernels.ContainsKey(n))
        {
            float[] kernel;

            if (n == 1)
                kernel = new float[] { 1.0f };
            else if (n == 3)
                kernel = new float[] { 0.25f, 0.5f, 0.25f };
            else if (n == 5)
                kernel = new float[] { 0.0625f, 0.25f, 0.375f, 0.25f, 0.0625f };
            else if (n == 7)
                kernel = new float[] { 0.03125f, 0.109375f, 0.21875f, 0.28125f, 0.21875f, 0.109375f, 0.03125f };
            else
                kernel = new float[n];

            float sigma = ((n - 1) * 0.5f - 1) * 0.3f + 0.8f;
            float scale2X = -0.5f / (sigma * sigma);
            float sum = 0;

            int i;
            for (i = 0; i < n; i++)
            {
                double x = i - (n - 1) * 0.5;
                double t = n <= 7 ? kernel[i] : Math.Exp(scale2X * x * x);
                kernel[i] = (float)t;
                sum += kernel[i];
            }

            sum = 1 / sum;

            for (i = 0; i < n; i++)
                kernel[i] = kernel[i] * sum;

            gaussianKernels.Add(n, kernel);
        }

        return gaussianKernels[n];
    }

    public static void Bgra2Gray(byte[] src, byte[] dst)
    {
        int len = Math.Min(dst.Length, src.Length / 4);
        int ii, v;
        byte b, g, r;
        for (int i = 0; i < len; i++)
        {
            ii = 4 * i;
            b = src[ii];
            g = src[ii + 1];
            r = src[ii + 2];
            v = (r + g + b) / 3;
            dst[i] = (byte)v;
        }
    }

    public static void Gray2Bgra(byte[] src, byte[] dst)
    {
        int len = Math.Min(src.Length, dst.Length / 4);
        for (int i = 0; i < len; i++)
        {
            byte v = src[i];
            dst[4 * i] = v;
            dst[4 * i + 1] = v;
            dst[4 * i + 2] = v;
            dst[4 * i + 3] = 255;
        }
    }

    static byte[] gaussianBlurTemp = null;
    public static void GuassianBlur(byte[] src, byte[] dst, int width, int height, int ksize)
    {
        if (ksize < 1 || ksize % 2 != 1) return;

        float[] kernel = GetGaussianKernel(ksize);

        // use a temporary array in case src == dst
        // resize the static array if needed
        // note: not thread-safe!
        if (gaussianBlurTemp == null || gaussianBlurTemp.Length < dst.Length) gaussianBlurTemp = new byte[dst.Length];

        int x, y, i, xx, yy, ii;
        float sum;

        int halfKsize = (ksize - 1) / 2;

        // x direction
        for (y = 0; y < height; y++)
            for (x = 0; x < width; x++)
            {
                sum = 0;
                for (i = -halfKsize; i <= halfKsize; i++)
                {
                    xx = x + i;
                    if (xx >= 0 && xx < width)
                    {
                        ii = y * width + xx;
                        sum += src[ii] * kernel[i + halfKsize];
                    }
                }
                if (sum < 0) sum = 0;
                if (sum > 255) sum = 255;
                gaussianBlurTemp[y * width + x] = (byte)sum;
            }

        // y direction
        for (y = 0; y < height; y++)
            for (x = 0; x < width; x++)
            {
                sum = 0;
                for (i = -halfKsize; i <= halfKsize; i++)
                {
                    yy = y + i;
                    if (yy >= 0 && yy < height)
                    {
                        ii = yy * width + x;
                        sum += src[ii] * kernel[i + halfKsize];
                    }
                }
                if (sum < 0) sum = 0;
                if (sum > 255) sum = 255;
                gaussianBlurTemp[y * width + x] = (byte)sum;
            }

        // copy the temporary array to the destination
        //for (i = 0; i < dst.Length; i++) dst[i] = gaussianBlurTemp[i];
        gaussianBlurTemp.CopyTo(dst, 0);
    }

    public static void Threshold(byte[] src, byte[] dst, byte threshold, byte value)
    {
        int n = Math.Min(src.Length, dst.Length);
        for (int i = 0; i < n; i++)
            dst[i] = src[i] < threshold ? (byte)0 : value;
    }

    // TODO: erode and dilate?

    static byte[] erodeTemp = null;
    public static void Erode(byte[] src, byte[] dst, int width, int height, int radius)
    {
        // use a temporary array in case src == dst
        // resize the static array if needed
        // note: not thread-safe!
        if (erodeTemp == null || erodeTemp.Length < dst.Length) erodeTemp = new byte[dst.Length];

        int x, y, xx, yy;
        bool goodPixel;
        for (y = 0; y < height; y++)
            for (x = 0; x < width; x++)
            {
                goodPixel = true;
                for (yy = y - radius; yy <= y + radius; yy++)
                {
                    for (xx = x - radius; xx <= x + radius; xx++)
                    {
                        if (yy >= 0 && yy < height && xx >= 0 && xx < width)
                        {
                            if (src[yy * width + xx] == 0)
                            {
                                goodPixel = false;
                                break;
                            }
                        }
                    }
                    if (!goodPixel) break;
                }

                erodeTemp[y * width + x] = (byte)(goodPixel ? 255 : 0);
            }

        erodeTemp.CopyTo(dst, 0);
    }

    public static void FindCenterOfMass(byte[] img, int width, int height, out float centerX, out float centerY)
    {
        centerX = 0;
        centerY = 0;

        int i, y, x;
        byte v;
        float n = 0;
        for (y = 0; y < height; y++)
            for (x = 0; x < width; x++)
            {
                i = y * width + x;
                v = img[i];

                if (v > 0)
                {
                    centerX += x;
                    centerY += y;
                    n++;
                }
            }

        if (n > 0)
        {
            centerX /= n;
            centerY /= n;
        }
        else
        {
            centerX = -1;
            centerY = -1;
        }
    }

    public static void LabelConnectedComponents(byte[] img, int width, int height, out short[] label, out Dictionary<short, int> labelCounts)
    {
        int index, x, y, recursiveIndex, recursiveX, recursiveY, tempIndex;

        label = new short[width * height];
        for (index = 0; index < width * height; index++) if (img[index] > 0) label[index] = -1;
        short currLabel = 0;
        int currLabelCount = 0;
        labelCounts = new Dictionary<short, int>();

        Stack<int> pixelsToLabel = new Stack<int>();

        for (y = 0; y < height; y++)
            for (x = 0; x < width; x++)
            {
                index = y * width + x;
                if (label[index] == -1)
                {
                    currLabel++;
                    pixelsToLabel.Push(index);
                    label[index] = currLabel;
                    currLabelCount = 1;

                    while (pixelsToLabel.Count > 0)
                    {
                        recursiveIndex = pixelsToLabel.Pop();
                        recursiveX = recursiveIndex % width;
                        recursiveY = recursiveIndex / width;

                        // check 8 neighbors (and make sure they're within the image boundaries)
                        if (recursiveY > 0)
                        {
                            if (recursiveX > 0 && label[(tempIndex = (recursiveY - 1) * width + (recursiveX - 1))] == -1)
                            {
                                pixelsToLabel.Push(tempIndex);
                                label[tempIndex] = currLabel;
                                currLabelCount++;
                            }
                            if (label[(tempIndex = (recursiveY - 1) * width + (recursiveX))] == -1)
                            {
                                pixelsToLabel.Push(tempIndex);
                                label[tempIndex] = currLabel;
                                currLabelCount++;
                            }
                            if (recursiveX + 1 < width && label[(tempIndex = (recursiveY - 1) * width + (recursiveX + 1))] == -1)
                            {
                                pixelsToLabel.Push(tempIndex);
                                label[tempIndex] = currLabel;
                                currLabelCount++;
                            }
                        }
                        if (recursiveX > 0 && label[(tempIndex = (recursiveY) * width + (recursiveX - 1))] == -1)
                        {
                            pixelsToLabel.Push(tempIndex);
                            label[tempIndex] = currLabel;
                            currLabelCount++;
                        }
                        if (recursiveX + 1 < width && label[(tempIndex = (recursiveY) * width + (recursiveX + 1))] == -1)
                        {
                            pixelsToLabel.Push(tempIndex);
                            label[tempIndex] = currLabel;
                            currLabelCount++;
                        }
                        if (recursiveY + 1 < height)
                        {
                            if (recursiveX > 0 && label[(tempIndex = (recursiveY + 1) * width + (recursiveX - 1))] == -1)
                            {
                                pixelsToLabel.Push(tempIndex);
                                label[tempIndex] = currLabel;
                                currLabelCount++;
                            }
                            if (label[(tempIndex = (recursiveY + 1) * width + (recursiveX))] == -1)
                            {
                                pixelsToLabel.Push(tempIndex);
                                label[tempIndex] = currLabel;
                                currLabelCount++;
                            }
                            if (recursiveX + 1 < width && label[(tempIndex = (recursiveY + 1) * width + (recursiveX + 1))] == -1)
                            {
                                pixelsToLabel.Push(tempIndex);
                                label[tempIndex] = currLabel;
                                currLabelCount++;
                            }
                        }
                    }

                    labelCounts.Add(currLabel, currLabelCount);
                }
            }
    }

    public static void FilterLargestComponent(short[] labels, Dictionary<short, int> labelCounts, byte[] dst)
    {
        if (labels.Length != dst.Length) return;

        short bestLabel = -1;
        int maxCount = 0;
        foreach (var labelCount in labelCounts) { if (labelCount.Value > maxCount) { maxCount = labelCount.Value; bestLabel = labelCount.Key; } }

        for (int i = 0; i < dst.Length; i++) if (labels[i] != bestLabel) dst[i] = 0;
    }
}
