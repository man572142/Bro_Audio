using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ami.BroAudio.Demo
{
    public class SceneReloader : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.CompareTag("Player"))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}