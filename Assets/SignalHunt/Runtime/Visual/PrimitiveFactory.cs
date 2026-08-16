using UnityEngine;

namespace SignalHunt.Visual
{
    public static class PrimitiveFactory
    {
        public static GameObject Cube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = true)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.Destroy(instance.GetComponent<Collider>());
            }

            return instance;
        }

        public static GameObject Sphere(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = true)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.Destroy(instance.GetComponent<Collider>());
            }

            return instance;
        }
    }
}
