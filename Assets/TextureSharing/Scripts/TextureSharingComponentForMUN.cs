using UnityEngine;
using UniRx;
using MonobitEngine;

namespace TextureSharing
{
    public class TextureSharingComponentForMUN : MonobitEngine.MonoBehaviour
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

        public void GetRawTextureDataFromMasterClient()
        {
            monobitView.RPC("GetRawTextureDataRPC", MonobitTargets.Host, MonobitNetwork.player);
        }

        //**************************************************************************
        // Client -> MasterClient (These methods are executed by the master client)
        //**************************************************************************
        [MunRPC]
        void GetRawTextureDataRPC(MonobitPlayer requestSender)
        {
            byte[] rawTextureData = texture.GetRawTextureData();

            int width = texture.width;
            int height = texture.height;
            int dataSize = rawTextureData.Length;
            int viewId = this.monobitView.viewID;

            Debug.Log("*************************");
            Debug.Log(" GetRawTextureDataRPC");
            Debug.Log(" RPC sender: " + requestSender.ID);
            Debug.Log(" Texture size: " + width + "x" + height + " = " + width*height + "px");
            Debug.Log(" RawTextureData: " + rawTextureData.Length + "bytes");
            Debug.Log("*************************");

            StreamTextureDataToRequestSender(rawTextureData, width, height, dataSize, viewId, requestSender);
        }

        void StreamTextureDataToRequestSender(byte[] rawTextureData, int width, int height, int dataSize, int viewId, MonobitPlayer targetPlayer)
        {
            Debug.Log("***********************************");
            Debug.Log(" StreamTextureDataToRequestSender  ");
            Debug.Log("***********************************");

            // Send info
            int[] textureInfo = new int[4];
            textureInfo[0] = viewId;
            textureInfo[1] = width;
            textureInfo[2] = height;
            textureInfo[3] = dataSize;
            monobitView.RPC("OnReceivedTextureInfo", targetPlayer, textureInfo);

            // Send raw data
            rawTextureData.ToObservable()
                .Buffer(bytePerMessage)
                .Subscribe(byteSubList =>
                {
                    byte[] sendData = new byte[byteSubList.Count];
                    byteSubList.CopyTo(sendData, 0);
                    monobitView.RPC("OnReceivedRawTextureDataStream", targetPlayer, sendData);
                });
        }

        //***************************************************************************
        // MasterClient -> Client (These methods are executed by the master client)
        //***************************************************************************
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
            Debug.Log(" RawTextureDataSize: " + data[3]);
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

            texture.LoadRawTextureData(this.receiveBuffer);
            texture.Apply();
            GetComponent<Renderer>().material.mainTexture = texture;
        }
    }
}