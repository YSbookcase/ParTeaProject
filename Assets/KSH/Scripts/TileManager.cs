using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;

namespace KSH
{
    public class TileManager : MonoBehaviour
    {
        public int redTileCount;
        public int blueTileCount;
        public event Action<int, int> OnTileCount;
        
        public static TileManager Instance;

        private void Awake()
        {
            if(Instance == null) // Instance가 null이면
            {
                Instance = this; // 할당
            }
            else // 이미 존재한다면
            {
                Destroy(gameObject); // 하나만 존재해야 하므로 제거
            }
            
            redTileCount = 0;
            blueTileCount = 0;
        }

        public void CountTile(Color before, Color after)
        {
            if (before == Color.red)
                redTileCount--;
            else if(before == Color.blue)
                blueTileCount--;
            
            if(after == Color.red)
                redTileCount++;
            else if(after == Color.blue)
                blueTileCount++;
            
            PhotonView photonView = GetComponent<PhotonView>();
            
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("PunRPC_CountTile", RpcTarget.All, redTileCount, blueTileCount); //타일 개수 동기화
        }
        
        [PunRPC]
        private void PunRPC_CountTile(int red, int blue)
        {
            redTileCount = red;
            blueTileCount = blue;
            OnTileCount?.Invoke(redTileCount, blueTileCount);
        }
    }    
}
