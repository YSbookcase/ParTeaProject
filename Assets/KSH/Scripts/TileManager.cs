using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KSH
{
    public class TileManager : MonoBehaviour
    {
        public int redTileCount;
        public int blueTileCount;
        
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
        }

        public void CountTile(Color color)
        {
            if(color == Color.red)
                redTileCount++;
            else if(color == Color.blue)
                blueTileCount++;
        }
    }    
}
