using UnityEngine;
using UnityEngine.Playables;

namespace PJW
{
    public class TimelineStarter : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;

        private void Start()
        {
            if (director != null)
            {
                //timeScale의 영향을 받지 않도록 설정
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                director.Play();
            }
        }
    }
}
