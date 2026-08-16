using SignalHunt.Visual;
using UnityEngine;

namespace SignalHunt.World
{
    public sealed class SignalPickupBurst : MonoBehaviour
    {
        private Transform[] _shards;
        private Vector3[] _directions;
        private float _elapsed;

        public static void Spawn(Vector3 position, Material material)
        {
            if (material == null)
            {
                return;
            }
            var root = new GameObject("Signal Pickup Burst");
            root.transform.position = position;
            var burst = root.AddComponent<SignalPickupBurst>();
            burst._shards = new Transform[12];
            burst._directions = new Vector3[12];
            for (var index = 0; index < burst._shards.Length; index++)
            {
                var angle = index * Mathf.PI * 2f / burst._shards.Length;
                var shard = PrimitiveFactory.Cube("Signal Shard", root.transform, Vector3.zero,
                    new Vector3(0.08f, 0.08f, 0.42f), material, false).transform;
                shard.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                burst._shards[index] = shard;
                burst._directions[index] = new Vector3(Mathf.Cos(angle), 0.35f, Mathf.Sin(angle)).normalized;
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            for (var index = 0; index < _shards.Length; index++)
            {
                _shards[index].localPosition = _directions[index] * (_elapsed * 5.5f);
                _shards[index].localScale = Vector3.one * Mathf.Clamp01(1f - _elapsed / 0.65f);
            }
            if (_elapsed >= 0.7f)
            {
                Destroy(gameObject);
            }
        }
    }
}
