using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WebCameraBlink
{
    public class CameraSelectUI : MonoBehaviour
    {
        public WebcamDisplay webcamDisplay;
        private UnityEngine.UI.Text textField;
        private List<CameraSelectUI.WebCam> devices;
        private int startDeviceIndex;
        private int currentDeviceIndex;

        public void Start()
        {
            webcamDisplay = transform.parent.GetComponentInChildren<WebcamDisplay>();
            textField = transform.Find("Text").GetComponent<UnityEngine.UI.Text>();
            devices = WebCamTexture.devices.Select(device => new CameraSelectUI.WebCam
            {
                webCam = device,
                deviceName = device.name
            }).ToList();
            WebCameraBlink.Instance.ModHelper.Console.WriteLine("devices: " + devices.Count);
            startDeviceIndex = devices.IndexOf(devices.LastOrDefault(device => !device.deviceName.Contains("Virtual")) ?? devices.LastOrDefault());
            currentDeviceIndex = startDeviceIndex;
            UpdateText();
        }

        private void UpdateText()
        {
            WebCameraBlink.Instance.ModHelper.Console.WriteLine("UpdateText: " + currentDeviceIndex.ToString() + ": " + devices[currentDeviceIndex].deviceName);
            if (devices.Count == 0)
            {
                textField.text = "Camera #?: No Camera Found";
                webcamDisplay.gameObject.SetActive(false);
                webcamDisplay.SetCamera(-1);
                return;
            }
            textField.text = "Camera #" + currentDeviceIndex.ToString() + ": " + devices[currentDeviceIndex].deviceName;
            webcamDisplay.gameObject.SetActive(true);
            webcamDisplay.SetCamera(currentDeviceIndex);
        }

        public void NextCamera()
        {
            currentDeviceIndex++;
            if (currentDeviceIndex >= devices.Count)
            {
                currentDeviceIndex = 0;
            }
            UpdateText();
        }

        public void PreviousCamera()
        {
            currentDeviceIndex--;
            if (currentDeviceIndex < 0)
            {
                currentDeviceIndex = devices.Count - 1;
            }
            UpdateText();
        }

        private class WebCam
        {
            public WebCamDevice webCam;
            public string deviceName;
        }

        public abstract class CameraSelectButton : Button
        {
            public CameraSelectUI cameraSelectUI;

            protected override void Start()
            {
                base.Start();
                cameraSelectUI = GetComponentInParent<CameraSelectUI>();
                onClick.AddListener(OnSelected);
            }

            protected abstract void OnSelected();
        }

        public class PreviousButton : CameraSelectButton
        {
            protected override void OnSelected()
            {
                cameraSelectUI.PreviousCamera();
            }
        }

        public class NextButton : CameraSelectButton
        {
            protected override void OnSelected()
            {
                cameraSelectUI.NextCamera();
            }
        }
    }

}
