using Reflex.Attributes;
using UnityEngine;

namespace Game.Feature.Input
{
    public class ScrollTest : MonoBehaviour
    {
        private ScrollController _scrollController;
        [Inject]
        private void Construct(ScrollController scrollController)
        {
            Debug.Log("ScrollC Test Construct");
            //this is a dummy class, to forecefully instantiate scrollController and check if it works
            _scrollController = scrollController;
        }
    }
}