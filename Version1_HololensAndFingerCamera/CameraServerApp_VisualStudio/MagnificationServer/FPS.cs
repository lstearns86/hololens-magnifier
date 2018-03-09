using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandSight
{
    public class FPS
    {
        static Dictionary<string, FPS> instances = new Dictionary<string, FPS>();
        public static FPS Instance(string name) { lock (instances) { if (!instances.ContainsKey(name)) instances[name] = new FPS(); } return instances[name]; }
        public static FPS Camera { get { return Instance("camera"); } }
        public static FPS Sensors { get { return Instance("sensors"); } }

        const int averageWindow = 5;

        static Stopwatch stopwatch = new Stopwatch();
        int frameCounter = 0, skipCounter = 0;
        float instantaneous = 0, average = 0, skipped = 0;
        long lastTime = 0, lastConsolidation = 0;
        Queue<int> frameCountQueue = new Queue<int>();
        Queue<int> skipCountQueue = new Queue<int>();

        public void Update()
        {
            try
            {
                if (!stopwatch.IsRunning) stopwatch.Start();

                long millis = stopwatch.ElapsedMilliseconds;

                // update instantaneous
                instantaneous = 1000.0f / (millis - lastTime);
                lastTime = millis;

                // update average
                frameCounter++;
                //Logging.IncrementFrameID();
                if (millis - lastConsolidation >= 1000)
                {
                    frameCountQueue.Enqueue(frameCounter);
                    if (frameCountQueue.Count > averageWindow) frameCountQueue.Dequeue();
                    average = 0;
                    foreach (int count in frameCountQueue) average += count;
                    average /= frameCountQueue.Count;
                    frameCounter = 0;

                    skipCountQueue.Enqueue(skipCounter);
                    if (skipCountQueue.Count > averageWindow) skipCountQueue.Dequeue();
                    skipped = 0;
                    foreach (int count in skipCountQueue) skipped += count;
                    skipped /= skipCountQueue.Count;
                    skipCounter = 0;

                    lastConsolidation += 1000;
                }
            }
            catch (Exception ex) { Debug.WriteLine("Error updating FPS: " + ex.ToString()); }
        }

        public void SkipFrame()
        {
            skipCounter++;
        }

        public float Instantaneous { get { return instantaneous; } }
        public float Average { get { return average; } }
        public float Skipped { get { return skipped; } }
        public float Total { get { return average + skipped; } }
    }
}
