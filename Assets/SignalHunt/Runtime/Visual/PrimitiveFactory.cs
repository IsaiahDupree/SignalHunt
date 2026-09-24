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
                DisableAndDestroyCollider(instance);
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
                DisableAndDestroyCollider(instance);
            }

            return instance;
        }

        public static GameObject Cylinder(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = true)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                DisableAndDestroyCollider(instance);
            }
            return instance;
        }

        public static GameObject Capsule(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = true)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                DisableAndDestroyCollider(instance);
            }
            return instance;
        }

        private static void DisableAndDestroyCollider(GameObject instance)
        {
            var primitiveCollider = instance.GetComponent<Collider>();
            if (primitiveCollider == null)
            {
                return;
            }
            // Generated visuals and the player are created in the same frame. Disable first so the
            // deferred Destroy cannot produce a one-frame physics collision that launches the player.
            primitiveCollider.enabled = false;
            Object.Destroy(primitiveCollider);
        }
    }
}
