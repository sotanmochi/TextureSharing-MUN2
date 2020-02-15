using UnityEngine;
using UniRx;
using MonobitEngine;

namespace TextureSharingForMUN
{
    public class TextureBroadcastComponent : MonobitEngine.MonoBehaviour
    {
        int bytePerMessage = 1000; // 1KBytes / Message
     
        Texture2D texture; // ★ Readable texture ★

        bool isReceiving;
        byte[] receiveBuffer;
        int totalDataSize;
        int currentReceivedDataSize;
        int receivedMessageCount;

        void Start()
        {
            texture = (Texture2D)GetComponent<Renderer>().material.mainTexture;
            try
            {
                texture.GetPixels32();
            }
            catch(UnityException e)
            {
                Debug.LogError("!! This texture is not readable !!");
            }
        }

        #region sender methods

        public void BroadcastTexture()
        {
            byte[] rawTextureData = texture.EncodeToPNG();

            int width = texture.width;
            int height = texture.height;
            int dataSize = rawTextureData.Length;
            int viewId = this.monobitView.viewID;

            Debug.Log("*************************");
            Debug.Log(" BroadcastTexture");
            Debug.Log(" Texture size: " + width + "x" + height + " = " + width*height + "px");
            Debug.Log(" RawTextureData: " + rawTextureData.Length + "bytes");
            Debug.Log("*************************");

            StreamTextureDataToOtherClients(rawTextureData, width, height, dataSize, viewId);
        }

        void StreamTextureDataToOtherClients(byte[] rawTextureData, int width, int height, int dataSize, int viewId)
        {
            Debug.Log("***********************************");
            Debug.Log(" StreamTextureDataToOthers  ");
            Debug.Log("***********************************");

            MonobitPlayer[] targetPlayers = MonobitNetwork.otherPlayersList;

            // Send info
            int[] textureInfo = new int[4];
            textureInfo[0] = viewId;
            textureInfo[1] = width;
            textureInfo[2] = height;
            textureInfo[3] = dataSize;
            monobitView.RPC("OnReceivedTextureInfo", targetPlayers, textureInfo);

            // Send raw data
            rawTextureData.ToObservable()
                .Buffer(bytePerMessage)
                .Subscribe(byteSubList =>
                {
                    byte[] sendData = new byte[byteSubList.Count];
                    byteSubList.CopyTo(sendData, 0);
                    monobitView.RPC("OnReceivedRawTextureDataStream", targetPlayers, sendData);
                });
        }

        #endregion

        #region receiver methods

        [MunRPC]
        void OnReceivedTextureInfo(int[] data)
        {
            int viewId = data[0];
            if (viewId != this.monobitView.viewID)
            {
                this.isReceiving = false;
                this.totalDataSize = 0;
                this.currentReceivedDataSize = 0;
                this.receivedMessageCount = 0;
                return;
            }

            this.isReceiving = true;
            this.currentReceivedDataSize = 0;
            this.receivedMessageCount = 0;

            int width = data[1];
            int height = data[2];
            int dataSize = data[3];
            this.totalDataSize = dataSize;
            this.receiveBuffer = new byte[dataSize];

            Debug.Log("*************************");
            Debug.Log(" OnReceivedTextureInfo");
            Debug.Log(" Texture size: " + width + "x" + height + "px");
            Debug.Log(" RawTextureDataSize: " + dataSize);
            Debug.Log("*************************");
        }

        [MunRPC]
        void OnReceivedRawTextureDataStream(byte[] data)
        {
            if (this.isReceiving)
            {
                data.CopyTo(this.receiveBuffer, this.currentReceivedDataSize);
                this.currentReceivedDataSize += data.Length;
                this.receivedMessageCount++;

                if (this.currentReceivedDataSize >= (this.totalDataSize))
                {
                    this.isReceiving = false;
                    this.currentReceivedDataSize = 0;
                    this.receivedMessageCount = 0;

                    OnReceivedRawTextureData();
                }
            }
        }

        void OnReceivedRawTextureData()
        {
            Debug.Log("********************************");
            Debug.Log(" OnReceivedRawTextureData ");
            Debug.Log("********************************");

            texture.LoadImage(this.receiveBuffer);
            texture.Apply();
            GetComponent<Renderer>().material.mainTexture = texture;
        }

        #endregion
    }
}