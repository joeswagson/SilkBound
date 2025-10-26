using UnityEngine;

namespace SilkBound.Behaviours
{
    public class SimpleInterpolator : MonoBehaviour
    {
        public Vector3 velocity;
        public float drag = 0.8f;

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;

            velocity *= Mathf.Pow(1 - drag, Time.deltaTime);
        }
    }
}
