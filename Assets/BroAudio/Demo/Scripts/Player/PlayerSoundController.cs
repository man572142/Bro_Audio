using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Demo
{
    public class PlayerSoundController : MonoBehaviour
    { 
        [SerializeField] AudioID _footstep = default;
        [SerializeField] AudioID _sandFootstep = default;
        [SerializeField] Terrain _terrain = null;
        private float[] _terrainTextureMix = new float[3];

        public void OnFootstep()
        {
            Vector3 playerPos = transform.position;
            if(Physics.Raycast(playerPos, Vector3.down, out RaycastHit hit) && hit.collider.gameObject.CompareTag(_terrain.tag))
            {
                GetTerrainTextureMix(playerPos, _terrainTextureMix);
                float baseGroundVol = _terrainTextureMix[0] + _terrainTextureMix[2];
                float sandVol = _terrainTextureMix[1];

                if(baseGroundVol > AudioConstant.MinVolume)
                {
                    BroAudio.Play(_footstep, playerPos).SetVolume(baseGroundVol);
                }

                if(sandVol > AudioConstant.MinVolume)
                {
                    BroAudio.Play(_sandFootstep, playerPos).SetVolume(sandVol);
                }
            }
            else
            {
                BroAudio.Play(_footstep, playerPos);
            }
        }

        private void GetTerrainTextureMix(Vector3 playerPos, float[] textureMix)
        {
            Vector3 playerOnTerrainPos = playerPos - _terrain.transform.position;
            Vector3 normailzedPos = new Vector3(playerOnTerrainPos.x / _terrain.terrainData.size.x, 0f, playerOnTerrainPos.z / _terrain.terrainData.size.z);

            int x = Mathf.RoundToInt(normailzedPos.x * _terrain.terrainData.alphamapWidth);
            int y = Mathf.RoundToInt(normailzedPos.z * _terrain.terrainData.alphamapHeight);

            float[,,] alphaMapValues = _terrain.terrainData.GetAlphamaps(x,y, 1,1);

            for(int i = 0; i < textureMix.Length;i++)
            {
                textureMix[i] = alphaMapValues[0, 0, i];
            }
            
        }
    } 
}