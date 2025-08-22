using OWML.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WebCameraBlink
{
    public class Calibration : MonoBehaviour
    {
        private bool collectingValues;
        private float currentBlink;
        private float previousBlink;
        private float priorBlink;
        private float calibrateOffset;
        private List<float> peaks = new List<float>();
        private List<float> valleys = new List<float>();

        private void Start()
        {
        }

        private void Update()
        {
            if (collectingValues && currentBlink != PipeReceiver.blinkValue)
            {
                priorBlink = previousBlink;
                previousBlink = currentBlink;
                currentBlink = PipeReceiver.blinkValue;
                if (previousBlink > currentBlink && previousBlink > priorBlink)
                {
                    peaks.Add(previousBlink);
                }
                if (previousBlink < currentBlink && previousBlink < priorBlink)
                {
                    valleys.Add(previousBlink);
                }
            }
        }

        private void Calibrate()
        {
            // Sort peaks descending, valleys ascending
            peaks.Sort((a, b) => b.CompareTo(a));
            valleys.Sort();

            // Filter out outlier peaks that are too low
            for (int i = peaks.Count - 1; i > 0; i--)
            {
                if (peaks[i] < peaks[1] - 0.2f)
                {
                    peaks.RemoveAt(i);
                }
            }

            // Filter out outlier valleys that are too high
            for (int i = valleys.Count - 1; i > 0; i--)
            {
                if (valleys[i] > valleys[1] + 0.2f)
                {
                    valleys.RemoveAt(i);
                }
            }

            // Compute averages
            float peakSum = 0;
            float valleySum = 0;

            foreach (float peak in peaks) peakSum += peak;
            foreach (float valley in valleys) valleySum += valley;

            float avgPeak = peakSum / peaks.Count;
            float avgValley = valleySum / valleys.Count;

            WebCameraBlink.Instance.ModHelper.Console.WriteLine($"Peaks average = {avgPeak}", MessageType.Debug);
            WebCameraBlink.Instance.ModHelper.Console.WriteLine($"Valleys average = {avgValley}", MessageType.Debug);

            // Compute threshold as midpoint between average peak and valley, plus any calibration offset
            float blinkThreshold = (avgPeak + avgValley) / 2f;
            WebCameraBlink.blinkingThreshhold = blinkThreshold + calibrateOffset;

            WebCameraBlink.Instance.ModHelper.Console.WriteLine($"Final Blink Threshold = {WebCameraBlink.blinkingThreshhold}", MessageType.Debug);
        }

        public void StartCalibration(float time)
        {
            StartCoroutine(CollectValues(time));
        }

        public void AdjustCalibrateOffset(float value)
        {
            calibrateOffset += value;
        }

        private IEnumerator CollectValues(float time)
        {
            collectingValues = true;
            yield return new WaitForSeconds(time);
            collectingValues = false;
            Calibrate();
            yield break;
        }
    }

}
