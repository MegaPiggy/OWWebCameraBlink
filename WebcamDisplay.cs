using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WebCameraBlink
{
    public class WebcamDisplay : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent cameraSelected;
        [SerializeField]
        private UnityEvent noCameraSelected;
        private RawImage image;
        private WebCamTexture tex;
        private int camIndex = -1;

        private void Awake()
        {
            image = GetComponent<RawImage>();
            image.enabled = false;
        }

        public void StopRendering()
        {
            if (tex != null)
            {
                tex.Stop();
            }
        }

        public void SetCamera(int index)
        {
            if (index < 0 || index > WebCamTexture.devices.Length)
            {
                if (tex != null)
                {
                    tex.Stop();
                }
                image.enabled = false;
                noCameraSelected?.Invoke();
            }
            else
            {
                if (tex != null)
                {
                    tex.Stop();
                }
                image.enabled = true;
                WebCamDevice[] devices = WebCamTexture.devices;
                tex = new WebCamTexture(devices[index].name);
                WebCameraBlink.Instance.ModHelper.Console.WriteLine(index + ": " + devices[index].name);
                image.texture = tex;
                tex.Play();
                cameraSelected?.Invoke();
            }
            camIndex = index;
            SetCameraIndex();
        }

        public void SetCameraIndex()
        {
            PipeReceiver.cameraID = camIndex;
        }

        private void OnDestroy()
        {
            StopRendering();
        }
    }

}
